using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
}

public class TableMetadata
{
    private static readonly ConcurrentDictionary<Type, TableMetadata> Cache = new();

    public Type ClrType { get; }
    public string TableName { get; }
    public List<ColumnInfo> Columns { get; }
    public ColumnInfo PrimaryKey { get; }
    public List<NavigationInfo> Navigations { get; }

    private TableMetadata(Type clrType, string tableName, List<ColumnInfo> columns, List<NavigationInfo> navigations)
    {
        ClrType = clrType;
        TableName = tableName;
        Columns = columns;
        PrimaryKey = columns.FirstOrDefault(c => c.IsPrimaryKey);
        Navigations = navigations;
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

                columns.Add(new ColumnInfo
                {
                    PropertyName = prop.Name,
                    ColumnName = colName,
                    ClrType = prop.PropertyType,
                    IsPrimaryKey = prop.GetCustomAttribute<PrimaryKeyAttribute>() != null,
                    IsAutoIncrement = prop.GetCustomAttribute<AutoIncrementAttribute>() != null,
                    OverrideSqlType = colTypeAttr?.SqlType,
                    PropertyInfo = prop,
                    Converter = converter,
                    ForeignKey = fkAttr
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
