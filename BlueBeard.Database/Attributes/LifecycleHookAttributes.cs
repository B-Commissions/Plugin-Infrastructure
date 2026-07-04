using System;

namespace BlueBeard.Database.Attributes;

/// <summary>
/// The DbSet operation stage a lifecycle hook runs at.
/// </summary>
public enum HookKind
{
    BeforeInsert,
    AfterInsert,
    BeforeUpdate,
    AfterUpdate,
    BeforeDelete,
    AfterDelete
}

/// <summary>
/// Base for the six lifecycle hook attributes. Place on instance methods of an entity:
///
/// <code>
/// [BeforeInsert] private void Stamp() => CreatedAt = DateTime.UtcNow;      // entity-level
/// [BeforeUpdate("balance")] private void Clamp(decimal value) { ... }      // column-targeted
/// </code>
///
/// Entity-level hooks are parameterless; column-targeted hooks take exactly one parameter
/// of the column's CLR type (validated when the entity's metadata is built — a mismatch
/// throws at registration, not at query time). The column target matches the column name
/// (the [Column("...")] literal, falling back to the property name); column-name literals
/// survive obfuscation where property-name strings do not.
///
/// Methods may return <c>void</c> or <c>Task</c>. Before* hooks run before the SQL is sent,
/// so entity mutations they make are written; After* hooks run only after success.
/// Hooks fire on instance-based operations (insert/update/delete of an entity, including
/// range variants, per entity). Expression-based deletes bypass hooks — no instances exist.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class LifecycleHookAttribute : Attribute
{
    /// <summary>Target column name; null for entity-level hooks.</summary>
    public string Column { get; }

    public abstract HookKind Kind { get; }

    protected LifecycleHookAttribute() { }
    protected LifecycleHookAttribute(string column) => Column = column;
}

public sealed class BeforeInsertAttribute : LifecycleHookAttribute
{
    public override HookKind Kind => HookKind.BeforeInsert;
    public BeforeInsertAttribute() { }
    public BeforeInsertAttribute(string column) : base(column) { }
}

public sealed class AfterInsertAttribute : LifecycleHookAttribute
{
    public override HookKind Kind => HookKind.AfterInsert;
    public AfterInsertAttribute() { }
    public AfterInsertAttribute(string column) : base(column) { }
}

public sealed class BeforeUpdateAttribute : LifecycleHookAttribute
{
    public override HookKind Kind => HookKind.BeforeUpdate;
    public BeforeUpdateAttribute() { }
    public BeforeUpdateAttribute(string column) : base(column) { }
}

public sealed class AfterUpdateAttribute : LifecycleHookAttribute
{
    public override HookKind Kind => HookKind.AfterUpdate;
    public AfterUpdateAttribute() { }
    public AfterUpdateAttribute(string column) : base(column) { }
}

public sealed class BeforeDeleteAttribute : LifecycleHookAttribute
{
    public override HookKind Kind => HookKind.BeforeDelete;
    public BeforeDeleteAttribute() { }
    public BeforeDeleteAttribute(string column) : base(column) { }
}

public sealed class AfterDeleteAttribute : LifecycleHookAttribute
{
    public override HookKind Kind => HookKind.AfterDelete;
    public AfterDeleteAttribute() { }
    public AfterDeleteAttribute(string column) : base(column) { }
}
