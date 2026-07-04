using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySqlConnector;

namespace BlueBeard.Database;

/// <summary>
/// Composable read query over a table:
///
/// <code>
/// var top = await db.Table&lt;PlayerData&gt;().Query()
///     .Where(p =&gt; p.Kills &gt; 100)
///     .OrderByDescending(p =&gt; p.Kills)
///     .Take(10)
///     .ToListAsync();
/// </code>
///
/// Multiple <c>Where</c> calls AND together. Ordering calls append in sequence
/// (first call is the primary sort). Immutable inputs are translated eagerly, so
/// translation errors surface at build time, not execution.
/// </summary>
public sealed class DbQuery<T> where T : new()
{
    private readonly DbSet<T> _set;
    private readonly List<string> _whereClauses = [];
    private readonly List<object> _parameters = [];
    private readonly List<string> _orderings = [];
    private int? _take;
    private int? _skip;

    internal DbQuery(DbSet<T> set) => _set = set;

    public DbQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        var (sql, parameters) = SqlWhereVisitor.Translate(predicate);
        // Re-number the clause's parameters after those already collected so
        // multiple Where calls never collide.
        var offset = _parameters.Count;
        sql = Regex.Replace(sql, @"@p(\d+)", m => $"@p{int.Parse(m.Groups[1].Value) + offset}");
        _whereClauses.Add(sql);
        _parameters.AddRange(parameters);
        return this;
    }

    public DbQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> key) => AddOrdering(key, "ASC");

    public DbQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> key) => AddOrdering(key, "DESC");

    /// <summary>Secondary sort — identical to calling OrderBy again; reads better after one.</summary>
    public DbQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> key) => AddOrdering(key, "ASC");

    public DbQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> key) => AddOrdering(key, "DESC");

    public DbQuery<T> Take(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        _take = count;
        return this;
    }

    public DbQuery<T> Skip(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        _skip = count;
        return this;
    }

    public Task<List<T>> ToListAsync() =>
        _set.QueryInternalAsync(BuildSelect(), Bind);

    public Task<List<T>> ToListAsync(BbTransaction transaction) =>
        _set.QueryInternalAsync(BuildSelect(), Bind, transaction);

    public async Task<T> FirstOrDefaultAsync()
    {
        var savedTake = _take;
        _take = 1;
        try
        {
            var results = await ToListAsync();
            return results.Count > 0 ? results[0] : default;
        }
        finally
        {
            _take = savedTake;
        }
    }

    public async Task<long> CountAsync()
    {
        var sql = $"SELECT COUNT(*) FROM {SqlIdentifier.Quote(_set.Metadata.TableName)}{BuildWhere()};";
        return await ScalarAsync(sql);
    }

    public async Task<bool> AnyAsync()
    {
        var sql = $"SELECT EXISTS(SELECT 1 FROM {SqlIdentifier.Quote(_set.Metadata.TableName)}{BuildWhere()});";
        return await ScalarAsync(sql) != 0;
    }

    // -----------------------------------------------------------------------

    private DbQuery<T> AddOrdering<TKey>(Expression<Func<T, TKey>> key, string direction)
    {
        var body = key.Body;
        while (body is UnaryExpression u && u.NodeType == ExpressionType.Convert)
            body = u.Operand;

        if (body is not MemberExpression member || member.Expression is not ParameterExpression)
            throw new NotSupportedException(
                "OrderBy expects a direct property access, e.g. OrderBy(x => x.Score).");

        var columnName = _set.Metadata.GetColumnName(member.Member.Name);
        _orderings.Add($"{SqlIdentifier.Quote(columnName)} {direction}");
        return this;
    }

    private string BuildWhere() =>
        _whereClauses.Count == 0 ? "" : " WHERE " + string.Join(" AND ", _whereClauses);

    private string BuildSelect()
    {
        var sb = new StringBuilder($"SELECT * FROM {SqlIdentifier.Quote(_set.Metadata.TableName)}");
        sb.Append(BuildWhere());

        if (_orderings.Count > 0)
            sb.Append(" ORDER BY ").Append(string.Join(", ", _orderings));

        if (_take.HasValue || _skip.HasValue)
        {
            // MySQL requires LIMIT to use OFFSET; the huge literal is the documented idiom
            // for "no limit, offset only".
            sb.Append(" LIMIT ").Append(_take?.ToString() ?? "18446744073709551615");
            if (_skip is > 0) sb.Append(" OFFSET ").Append(_skip.Value);
        }

        sb.Append(';');
        return sb.ToString();
    }

    private void Bind(MySqlCommand cmd)
    {
        for (var i = 0; i < _parameters.Count; i++)
            cmd.Parameters.AddWithValue($"@p{i}", _parameters[i] ?? DBNull.Value);
    }

    private async Task<long> ScalarAsync(string sql)
    {
        using var conn = _set.ConnectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        Bind(cmd);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }
}
