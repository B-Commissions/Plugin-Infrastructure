# Converters

Converters bridge CLR types and SQL storage shapes that aren't covered by the built-in primitive map. They're the right answer when:

- A type isn't `int`/`long`/`string`/`bool`/`DateTime`/etc. (e.g., `Guid`, `byte[]`, `TimeSpan`, your own value type).
- You want to control how a CLR type is stored (e.g., `Guid` as `CHAR(36)` vs `BINARY(16)`).
- An existing column's storage shape doesn't match what your CLR type would produce by default.

Each converter is responsible for three things:

| Responsibility            | Method / Property         |
|---------------------------|---------------------------|
| Schema generation         | `DefaultSqlType`          |
| CLR -> DB parameter       | `ToProvider(clrValue)`    |
| Raw reader value -> CLR   | `FromProvider(rawValue)`  |

`FromProvider` deliberately receives the *raw* `reader.GetValue(ordinal)` result, not a typed getter result. This is what makes converters tolerant of storage drift — the converter decides what shapes it accepts.

---

## The IValueConverter interface

```csharp
namespace BlueBeard.Database.Converters;

public interface IValueConverter
{
    Type ClrType { get; }
    string DefaultSqlType { get; }
    object ToProvider(object clrValue);
    object FromProvider(object providerValue);
}
```

---

## Built-in converters

These are auto-registered at startup:

| Type        | Default SQL Type | Notes                                                   |
|-------------|------------------|---------------------------------------------------------|
| `Guid`      | `CHAR(36)`       | Reads accept `Guid`, `string`, or 16-byte `byte[]`.     |
| `byte[]`    | `VARBINARY(255)` | Reads accept `byte[]` or Base64 `string`.               |
| `TimeSpan`  | `BIGINT`         | Stored as `Ticks`. Reads accept any numeric type.       |

Opt-in (not auto-registered):

| Type   | Default SQL Type | Notes                                                  |
|--------|------------------|--------------------------------------------------------|
| `Guid` | `BINARY(16)`     | `GuidBinaryConverter` — apply via `[ColumnConverter]`. |

### Example: Guid as CHAR(36) (default)

```csharp
[Table("backpacks")]
public class Backpack
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }   // CHAR(36)
}
```

### Example: Guid as BINARY(16) (opt-in)

```csharp
[Table("backpacks")]
public class Backpack
{
    [PrimaryKey]
    [Column("id")]
    [ColumnConverter(typeof(GuidBinaryConverter))]
    public Guid Id { get; set; }   // BINARY(16)
}
```

`[ColumnConverter]` overrides the registry's default for that single property.

### Read tolerance

Both Guid converters accept `string`, `byte[]`, and native `Guid` shapes on read. So if you migrate one column from `CHAR(36)` to `BINARY(16)` (or vice versa), existing rows continue to read correctly during the transition.

---

## Registering custom converters

Implement `IValueConverter` and call `ValueConverters.Register(...)` once at startup, before the first entity is registered or queried:

```csharp
using BlueBeard.Database.Converters;

public class JsonConverter<T> : IValueConverter
{
    public Type ClrType => typeof(T);
    public string DefaultSqlType => "JSON";

    public object ToProvider(object clrValue) =>
        Newtonsoft.Json.JsonConvert.SerializeObject(clrValue);

    public object FromProvider(object providerValue) => providerValue switch
    {
        string s => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(s),
        byte[] b => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(System.Text.Encoding.UTF8.GetString(b)),
        _ => throw new InvalidCastException($"Cannot convert {providerValue?.GetType().Name} to {typeof(T).Name}")
    };
}

// At plugin startup:
ValueConverters.Register(new JsonConverter<MyComplexType>());
```

After registration, any property typed `MyComplexType` will use this converter automatically. You can override on a per-property basis with `[ColumnConverter]` if needed.

### Per-CLR-type vs per-column

`ValueConverters.Register` is keyed by CLR type — there can only be one *default* converter for a given type. To use a different converter for a specific column without changing the global default, use `[ColumnConverter(typeof(MyOtherConverter))]` on that property. This is exactly how `GuidBinaryConverter` works alongside the auto-registered `GuidConverter`.

---

## Mapping precedence

When generating DDL or binding parameters, the type to use is decided in this order:

1. `[ColumnType("...")]` if present (highest priority — raw SQL string)
2. `[ColumnConverter(typeof(...))]` if present
3. Registered converter for the property's CLR type
4. Built-in primitive map (`int`, `string`, etc.)
5. Enum fallback (`INT`)
6. `NotSupportedException` if none of the above match

---

## Where converters apply

Converters are applied at every site where a CLR value gets bound to a SQL parameter, and every site where a value gets read back from the reader. Concretely:

- `InsertAsync` — non-PK column values
- `UpdateAsync` — both SET clause values and the PK in the WHERE clause
- `DeleteAsync(entity)` — the PK in the WHERE clause
- `SqlWhereVisitor` — captured values on the value-side of any comparison against a converter-typed column
- `EntityReader` (the read path used by `QueryAsync`, `Where`, `FirstOrDefaultAsync`, `QuerySqlAsync`, and navigation loading)

The two raw-SQL escape hatches (`QuerySqlAsync` and `ExecuteSqlAsync`) do **not** apply converters to parameters you pass in — the column context isn't knowable. If you need converted values there, call `ToProvider` yourself before passing.

---

## Read fallback

Even when a property doesn't have a converter, the read path is tolerant of common type mismatches. If MySQL returns a `byte[]` for a `string` property (e.g. binary-collated column), the reader's typed `GetString` would throw `InvalidCastException`. The fallback path catches this and decodes the bytes as UTF-8.

For other type mismatches (e.g. `byte[]` returned for an `int` property), the fallback throws an `InvalidCastException` with a message pointing you toward `IValueConverter` or `[ColumnConverter]` as the fix.