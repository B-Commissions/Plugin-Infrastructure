using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueBeard.Database.Attributes;

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
}

public class TableMetadata
{
    private static readonly ConcurrentDictionary<Type, TableMetadata> Cache = new();
    public string TableName { get; }
    public List<ColumnInfo> Columns { get; }
    public ColumnInfo PrimaryKey { get; }

    private TableMetadata(string tableName, List<ColumnInfo> columns)
    {
        TableName = tableName;
        Columns = columns;
        PrimaryKey = columns.FirstOrDefault(c => c.IsPrimaryKey);
    }

    public static TableMetadata For<T>() => For(typeof(T));

    public static TableMetadata For(Type type)
    {
        return Cache.GetOrAdd(type, t =>
        {
            var tableAttr = t.GetCustomAttribute<TableAttribute>();
            var tableName = tableAttr?.Name ?? t.Name;
            var columns = new List<ColumnInfo>();
            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
                var colName = colAttr?.Name ?? prop.Name;
                var colTypeAttr = prop.GetCustomAttribute<ColumnTypeAttribute>();
                columns.Add(new ColumnInfo
                {
                    PropertyName = prop.Name,
                    ColumnName = colName,
                    ClrType = prop.PropertyType,
                    IsPrimaryKey = prop.GetCustomAttribute<PrimaryKeyAttribute>() != null,
                    IsAutoIncrement = prop.GetCustomAttribute<AutoIncrementAttribute>() != null,
                    OverrideSqlType = colTypeAttr?.SqlType,
                    PropertyInfo = prop
                });
            }
            return new TableMetadata(tableName, columns);
        });
    }

    public string GetColumnName(string propertyName)
    {
        return Columns.FirstOrDefault(c => c.PropertyName == propertyName)?.ColumnName ?? propertyName;
    }
}
