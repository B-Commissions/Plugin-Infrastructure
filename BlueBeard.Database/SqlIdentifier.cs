namespace BlueBeard.Database;

/// <summary>
/// Backtick-quotes SQL identifiers (table/column/index names) for safe interpolation
/// into DDL and DML. Identifiers come from developer-authored attributes and type names,
/// so this is hardening against malformed names, not user-input sanitization.
/// </summary>
internal static class SqlIdentifier
{
    public static string Quote(string identifier) =>
        "`" + identifier.Replace("`", "``") + "`";
}
