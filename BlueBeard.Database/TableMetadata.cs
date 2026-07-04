using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using BlueBeard.Database.Attributes;
using BlueBeard.Database.Converters;

namespace BlueBeard.Database;

public class ColumnInfo
{
    public string PropertyName { get; set; }
    public string ColumnName { get; set; }
    public Type ClrType { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsAutoIncrement { get; set; }
    public string OverrideSqlType { get; set; }
    public PropertyInfo PropertyInfo { get; set; }
    public IValueConverter Converter { get; set; }
    public ForeignKeyAttribute ForeignKey { get; set; }

    /// <summary>
    /// Resolved nullability: explicit attribute wins, then PK/AutoIncrement force NOT NULL,
    /// otherwise the historical default (nullable).
    /// </summary>
    public bool IsNullable { get; set; } = true;

    /// <summary>
    /// True only when the entity author declared nullability via [Required] or
    /// [Column(Nullable = ...)]. Schema diffing only enforces nullability drift for
    /// explicit declarations, so unannotated legacy schemas are never churned.
    /// </summary>
    public bool HasExplicitNullability { get; set; }

    /// <summary>Single-column UNIQUE constraint via [Unique].</summary>
    public bool IsUnique { get; set; }

    /// <summary>[MaxLength] sizing for string columns; null when unspecified.</summary>
    public MaxLengthAttribute MaxLength { get; set; }

    /// <summary>[DefaultValue] declaration; null when unspecified.</summary>
    public DefaultValueAttribute Default { get; set; }
}

/// <summary>
/// A secondary index derived from [Unique] and [Index] attributes. Matched against
/// INFORMATION_SCHEMA.STATISTICS by name; missing indexes are created, never dropped.
/// </summary>
public class IndexInfo
{
    public string Name { get; set; }
    public bool IsUnique { get; set; }
    public List<ColumnInfo> Columns { get; set; }
}

/// <summary>
/// A validated lifecycle hook discovered from a [Before*/After*] attribute. The invoker
/// is compiled once at metadata build — no per-call reflection.
/// </summary>
public class HookInfo
{
    public HookKind Kind { get; set; }

    /// <summary>Target column for column-level hooks; null for entity-level.</summary>
    public ColumnInfo TargetColumn { get; set; }

    public MethodInfo Method { get; set; }

    /// <summary>(entity, columnValue-or-null) → completion. Sync methods return Task.CompletedTask.</summary>
    internal Func<object, object, Task> Invoker { get; set; }
}

public class TableMetadata
{
    private static readonly ConcurrentDictionary<Type, TableMetadata> Cache = new();

    public Type ClrType { get; }
    public string TableName { get; }
    public List<ColumnInfo> Columns { get; }
    public ColumnInfo PrimaryKey { get; }
    public List<NavigationInfo> Navigations { get; }
    public List<IndexInfo> Indexes { get; }
    public List<HookInfo> Hooks { get; }

    private TableMetadata(Type clrType, string tableName, List<ColumnInfo> columns, List<NavigationInfo> navigations)
    {
        ClrType = clrType;
        TableName = tableName;
        Columns = columns;
        PrimaryKey = columns.FirstOrDefault(c => c.IsPrimaryKey);
        Navigations = navigations;
        Indexes = BuildIndexes(tableName, columns);
        Hooks = BuildHooks(clrType, columns);
    }

    public static TableMetadata For<T>() => For(typeof(T));

    public static TableMetadata For(Type type)
    {
        return Cache.GetOrAdd(type, t =>
        {
            var tableAttr = t.GetCustomAttribute<TableAttribute>();
            var tableName = tableAttr?.Name ?? t.Name;
            var columns = new List<ColumnInfo>();
            var navigations = new List<NavigationInfo>();

            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;

                // Navigation properties don't map to a column.
                var hasManyAttr = prop.GetCustomAttribute<HasManyAttribute>();
                if (hasManyAttr != null)
                {
                    var elementType = TryGetCollectionElementType(prop.PropertyType)
                        ?? throw new InvalidOperationException(
                            $"[HasMany] property '{t.Name}.{prop.Name}' must be List<T>, IList<T>, ICollection<T>, or IEnumerable<T>.");

                    navigations.Add(new NavigationInfo
                    {
                        PropertyInfo = prop,
                        Kind = NavigationKind.HasMany,
                        ElementType = elementType,
                        ForeignKeyProperty = hasManyAttr.ForeignKeyProperty
                    });
                    continue;
                }

                var belongsToAttr = prop.GetCustomAttribute<BelongsToAttribute>();
                if (belongsToAttr != null)
                {
                    navigations.Add(new NavigationInfo
                    {
                        PropertyInfo = prop,
                        Kind = NavigationKind.BelongsTo,
                        ElementType = prop.PropertyType,
                        LocalKeyProperty = belongsToAttr.LocalKeyProperty
                    });
                    continue;
                }

                // Regular mapped column.
                var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
                var colName = colAttr?.Name ?? prop.Name;
                var colTypeAttr = prop.GetCustomAttribute<ColumnTypeAttribute>();
                var converterAttr = prop.GetCustomAttribute<ColumnConverterAttribute>();
                var fkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>();

                IValueConverter converter;
                if (converterAttr != null)
                    converter = (IValueConverter)Activator.CreateInstance(converterAttr.ConverterType);
                else
                    ValueConverters.TryGet(prop.PropertyType, out converter);

                var isPrimaryKey = prop.GetCustomAttribute<PrimaryKeyAttribute>() != null;
                var isAutoIncrement = prop.GetCustomAttribute<AutoIncrementAttribute>() != null;

                // Explicit declaration ([Required] / [Column(Nullable = ...)]) wins; [Required]
                // beats a conflicting Nullable = true. PK/AutoIncrement are implicitly NOT NULL
                // (MySQL enforces this anyway) but don't count as an explicit declaration.
                var required = prop.GetCustomAttribute<RequiredAttribute>() != null;
                var explicitNullable = required ? false : colAttr?.NullableExplicit;
                var hasExplicit = required || colAttr?.NullableExplicit != null;

                columns.Add(new ColumnInfo
                {
                    PropertyName = prop.Name,
                    ColumnName = colName,
                    ClrType = prop.PropertyType,
                    IsPrimaryKey = isPrimaryKey,
                    IsAutoIncrement = isAutoIncrement,
                    OverrideSqlType = colTypeAttr?.SqlType,
                    PropertyInfo = prop,
                    Converter = converter,
                    ForeignKey = fkAttr,
                    IsNullable = hasExplicit ? explicitNullable.Value : !(isPrimaryKey || isAutoIncrement),
                    HasExplicitNullability = hasExplicit,
                    IsUnique = prop.GetCustomAttribute<UniqueAttribute>() != null,
                    MaxLength = prop.GetCustomAttribute<MaxLengthAttribute>(),
                    Default = prop.GetCustomAttribute<DefaultValueAttribute>()
                });
            }

            return new TableMetadata(t, tableName, columns, navigations);
        });
    }

    public string GetColumnName(string propertyName)
    {
        return Columns.FirstOrDefault(c => c.PropertyName == propertyName)?.ColumnName ?? propertyName;
    }

    public ColumnInfo GetColumnByPropertyName(string propertyName)
    {
        return Columns.FirstOrDefault(c => c.PropertyName == propertyName);
    }

    // Durable under obfuscation: locate the column that is a [ForeignKey] pointing at
    // the given referenced entity type. Type tokens survive renaming; property-name
    // strings do not. Returns null on no match OR ambiguity (>1 FK to the same type),
    // so callers fall through to their existing name-based error.
    public ColumnInfo GetForeignKeyColumnTo(Type referencedType)
    {
        ColumnInfo found = null;
        foreach (var c in Columns)
        {
            if (c.ForeignKey == null || c.ForeignKey.ReferencedType != referencedType) continue;
            if (found != null) return null; // ambiguous — refuse to guess
            found = c;
        }
        return found;
    }

    private static List<IndexInfo> BuildIndexes(string tableName, List<ColumnInfo> columns)
    {
        var indexes = new List<IndexInfo>();

        // [Unique] — single-column unique index, deterministic name.
        foreach (var col in columns.Where(c => c.IsUnique && !c.IsPrimaryKey))
        {
            indexes.Add(new IndexInfo
            {
                Name = $"ux_{tableName}_{col.ColumnName}",
                IsUnique = true,
                Columns = [col]
            });
        }

        // [Index] — plain single-column, or composite when Group is shared.
        var composites = new Dictionary<string, List<(ColumnInfo Col, IndexAttribute Attr, int DeclOrder)>>();
        var declOrder = 0;
        foreach (var col in columns)
        {
            foreach (var attr in col.PropertyInfo.GetCustomAttributes<IndexAttribute>())
            {
                if (string.IsNullOrEmpty(attr.Group))
                {
                    indexes.Add(new IndexInfo
                    {
                        Name = $"ix_{tableName}_{col.ColumnName}",
                        IsUnique = attr.Unique,
                        Columns = [col]
                    });
                }
                else
                {
                    if (!composites.TryGetValue(attr.Group, out var members))
                        composites[attr.Group] = members = [];
                    members.Add((col, attr, declOrder));
                }
            }
            declOrder++;
        }

        foreach (var kvp in composites)
        {
            var ordered = kvp.Value
                .OrderBy(m => m.Attr.Order)
                .ThenBy(m => m.DeclOrder)
                .ToList();
            indexes.Add(new IndexInfo
            {
                Name = $"ix_{tableName}_{kvp.Key}",
                IsUnique = ordered.Any(m => m.Attr.Unique),
                Columns = ordered.Select(m => m.Col).ToList()
            });
        }

        return indexes;
    }

    private static List<HookInfo> BuildHooks(Type type, List<ColumnInfo> columns)
    {
        var hooks = new List<HookInfo>();

        // Includes inherited public/protected methods; private hooks work on the declaring
        // type itself (the overwhelmingly common case). MetadataToken keeps declaration order
        // deterministic.
        var methods = type
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.MetadataToken);

        foreach (var method in methods)
        {
            foreach (var attr in method.GetCustomAttributes<LifecycleHookAttribute>())
            {
                hooks.Add(BuildHook(type, columns, method, attr));
            }
        }

        return hooks;
    }

    private static HookInfo BuildHook(Type type, List<ColumnInfo> columns, MethodInfo method, LifecycleHookAttribute attr)
    {
        string Describe() => $"[{attr.GetType().Name.Replace("Attribute", "")}] on {type.Name}.{method.Name}";

        if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
            throw new InvalidOperationException(
                $"{Describe()}: hook methods must return void or Task, not {method.ReturnType.Name}.");

        var parameters = method.GetParameters();
        ColumnInfo targetColumn = null;

        if (attr.Column == null)
        {
            if (parameters.Length != 0)
                throw new InvalidOperationException(
                    $"{Describe()}: entity-level hooks must be parameterless. " +
                    $"To receive a column value, target the column: [{attr.GetType().Name.Replace("Attribute", "")}(\"column_name\")].");
        }
        else
        {
            // Column-name literal first (durable under obfuscation), property name as fallback.
            targetColumn = columns.FirstOrDefault(c => string.Equals(c.ColumnName, attr.Column, StringComparison.OrdinalIgnoreCase))
                ?? columns.FirstOrDefault(c => string.Equals(c.PropertyName, attr.Column, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"{Describe()}: target column '{attr.Column}' is not a mapped column on {type.Name}. " +
                    $"Use the column name (the [Column(\"...\")] literal) — property names do not survive obfuscation.");

            if (parameters.Length != 1)
                throw new InvalidOperationException(
                    $"{Describe()}: column-targeted hooks must take exactly one parameter of the column's CLR type " +
                    $"({FriendlyTypeName(targetColumn.ClrType)}).");

            var paramType = parameters[0].ParameterType;
            var columnType = targetColumn.ClrType;
            var accepted = paramType == columnType
                || paramType == Nullable.GetUnderlyingType(columnType)  // int hook for int? column
                || Nullable.GetUnderlyingType(paramType) == columnType  // int? hook for int column
                || paramType.IsAssignableFrom(columnType);
            if (!accepted)
                throw new InvalidOperationException(
                    $"{Describe()}: parameter type {FriendlyTypeName(paramType)} does not match column " +
                    $"'{targetColumn.ColumnName}' of type {FriendlyTypeName(columnType)}.");
        }

        return new HookInfo
        {
            Kind = attr.Kind,
            TargetColumn = targetColumn,
            Method = method,
            Invoker = CompileInvoker(type, method, targetColumn == null ? null : method.GetParameters()[0].ParameterType)
        };
    }

    /// <summary>
    /// Compile (object entity, object arg) => Task once per hook so dispatch never reflects.
    /// </summary>
    private static Func<object, object, Task> CompileInvoker(Type type, MethodInfo method, Type paramType)
    {
        var entityParam = Expression.Parameter(typeof(object), "entity");
        var argParam = Expression.Parameter(typeof(object), "arg");

        var call = paramType == null
            ? Expression.Call(Expression.Convert(entityParam, type), method)
            : Expression.Call(Expression.Convert(entityParam, type), method, Expression.Convert(argParam, paramType));

        Expression body = method.ReturnType == typeof(Task)
            ? call
            : Expression.Block(call, Expression.Constant(Task.CompletedTask, typeof(Task)));

        return Expression.Lambda<Func<object, object, Task>>(body, entityParam, argParam).Compile();
    }

    private static string FriendlyTypeName(Type t)
    {
        var underlying = Nullable.GetUnderlyingType(t);
        return underlying != null ? underlying.Name + "?" : t.Name;
    }

    private static Type TryGetCollectionElementType(Type t)
    {
        if (!t.IsGenericType) return null;
        var gen = t.GetGenericTypeDefinition();
        if (gen == typeof(List<>) || gen == typeof(IList<>) ||
            gen == typeof(ICollection<>) || gen == typeof(IEnumerable<>))
            return t.GetGenericArguments()[0];
        return null;
    }
}
