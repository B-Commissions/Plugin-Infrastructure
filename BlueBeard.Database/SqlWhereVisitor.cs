using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace BlueBeard.Database;

public class SqlWhereVisitor(TableMetadata metadata) : ExpressionVisitor
{
    private readonly StringBuilder _sb = new();
    private readonly List<object> _parameters = [];

    /// <summary>
    /// Tracks which column the current "value-side" of a comparison is being compared against,
    /// so AddParameter knows which converter to apply.
    /// </summary>
    private ColumnInfo _currentColumn;

    public string Sql => _sb.ToString();
    public List<object> Parameters => _parameters;

    public static (string sql, List<object> parameters) Translate<T>(Expression<Func<T, bool>> predicate)
    {
        var metadata = TableMetadata.For<T>();
        var visitor = new SqlWhereVisitor(metadata);
        visitor.Visit(predicate.Body);
        return (visitor.Sql, visitor.Parameters);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        _sb.Append("(");

        // For comparisons, identify which side is a column reference so we can apply the
        // OTHER side's converter when visiting the value side.
        var leftCol = TryGetColumn(node.Left);
        var rightCol = TryGetColumn(node.Right);
        var saved = _currentColumn;
        var isComparison = IsComparison(node.NodeType);

        // When visiting left, value-side converter (if any) comes from the right's column.
        if (isComparison) _currentColumn = rightCol;
        Visit(node.Left);
        _currentColumn = saved;

        switch (node.NodeType)
        {
            case ExpressionType.Equal:
                if (IsNullConstant(node.Right)) { _sb.Append(" IS NULL)"); return node; }
                _sb.Append(" = "); break;
            case ExpressionType.NotEqual:
                if (IsNullConstant(node.Right)) { _sb.Append(" IS NOT NULL)"); return node; }
                _sb.Append(" != "); break;
            case ExpressionType.LessThan:           _sb.Append(" < ");  break;
            case ExpressionType.GreaterThan:        _sb.Append(" > ");  break;
            case ExpressionType.LessThanOrEqual:    _sb.Append(" <= "); break;
            case ExpressionType.GreaterThanOrEqual: _sb.Append(" >= "); break;
            case ExpressionType.AndAlso:            _sb.Append(" AND "); break;
            case ExpressionType.OrElse:             _sb.Append(" OR ");  break;
            default:
                throw new NotSupportedException($"Binary operator '{node.NodeType}' is not supported.");
        }

        // When visiting right, value-side converter comes from the left's column.
        if (isComparison) _currentColumn = leftCol;
        Visit(node.Right);
        _currentColumn = saved;

        _sb.Append(")");
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ParameterExpression)
        {
            var columnName = metadata.GetColumnName(node.Member.Name);
            _sb.Append(SqlIdentifier.Quote(columnName));
            return node;
        }
        AddParameter(EvaluateExpression(node));
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // string.IsNullOrEmpty(x.Name)  ->  (`name` IS NULL OR `name` = '')
        if (node.Method.IsStatic && node.Method.DeclaringType == typeof(string) &&
            node.Method.Name == nameof(string.IsNullOrEmpty))
        {
            var col = TryGetColumn(node.Arguments[0])
                ?? throw new NotSupportedException("string.IsNullOrEmpty is only supported on entity properties.");
            var quoted = SqlIdentifier.Quote(col.ColumnName);
            _sb.Append($"({quoted} IS NULL OR {quoted} = '')");
            return node;
        }

        // x.Name.Contains/StartsWith/EndsWith("...")  ->  `name` LIKE @p (wildcards escaped)
        if (node.Object != null && node.Method.DeclaringType == typeof(string) &&
            node.Arguments.Count == 1 &&
            node.Method.Name is nameof(string.Contains) or nameof(string.StartsWith) or nameof(string.EndsWith))
        {
            var col = TryGetColumn(node.Object);
            if (col != null)
            {
                var raw = EvaluateExpression(node.Arguments[0]) as string
                    ?? throw new NotSupportedException($"{node.Method.Name} requires a non-null string argument.");
                var escaped = raw.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                var pattern = node.Method.Name switch
                {
                    nameof(string.StartsWith) => escaped + "%",
                    nameof(string.EndsWith) => "%" + escaped,
                    _ => "%" + escaped + "%"
                };

                _sb.Append($"{SqlIdentifier.Quote(col.ColumnName)} LIKE @p{_parameters.Count}");
                _parameters.Add(pattern);
                return node;
            }
        }

        // ids.Contains(x.Id)  /  Enumerable.Contains(ids, x.Id)  ->  `id` IN (@p0, @p1, ...)
        if (node.Method.Name == nameof(Enumerable.Contains))
        {
            Expression collectionExpr = null, itemExpr = null;
            if (node.Object == null && node.Arguments.Count == 2 && node.Method.DeclaringType == typeof(Enumerable))
            {
                collectionExpr = node.Arguments[0];
                itemExpr = node.Arguments[1];
            }
            else if (node.Object != null && node.Arguments.Count == 1)
            {
                collectionExpr = node.Object;
                itemExpr = node.Arguments[0];
            }

            if (collectionExpr != null && TryGetColumn(itemExpr) is { } col)
            {
                var values = ((IEnumerable)EvaluateExpression(collectionExpr))
                    ?.Cast<object>().ToList()
                    ?? throw new NotSupportedException("Contains requires a non-null collection.");

                if (values.Count == 0)
                {
                    // IN () is invalid SQL; an empty set matches nothing.
                    _sb.Append("(1 = 0)");
                    return node;
                }

                var placeholders = new List<string>(values.Count);
                foreach (var value in values)
                {
                    var v = value != null && col.Converter != null ? col.Converter.ToProvider(value) : value;
                    placeholders.Add($"@p{_parameters.Count}");
                    _parameters.Add(v);
                }
                _sb.Append($"{SqlIdentifier.Quote(col.ColumnName)} IN ({string.Join(", ", placeholders)})");
                return node;
            }
        }

        // Anything else that doesn't touch the entity parameter is a value — evaluate it.
        if (!ReferencesParameter(node))
        {
            AddParameter(EvaluateExpression(node));
            return node;
        }

        throw new NotSupportedException(
            $"Method '{node.Method.DeclaringType?.Name}.{node.Method.Name}' cannot be translated to SQL. " +
            "Supported: string Contains/StartsWith/EndsWith, string.IsNullOrEmpty, collection Contains.");
    }

    private static bool ReferencesParameter(Expression expr)
    {
        var finder = new ParameterFinder();
        finder.Visit(expr);
        return finder.Found;
    }

    private sealed class ParameterFinder : ExpressionVisitor
    {
        public bool Found { get; private set; }
        protected override Expression VisitParameter(ParameterExpression node)
        {
            Found = true;
            return node;
        }
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        AddParameter(node.Value);
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Convert) { Visit(node.Operand); return node; }
        if (node.NodeType == ExpressionType.Not) { _sb.Append("NOT "); Visit(node.Operand); return node; }
        return base.VisitUnary(node);
    }

    private void AddParameter(object value)
    {
        if (value != null && _currentColumn?.Converter != null)
            value = _currentColumn.Converter.ToProvider(value);
        _sb.Append($"@p{_parameters.Count}");
        _parameters.Add(value);
    }

    private static bool IsComparison(ExpressionType t) =>
        t is ExpressionType.Equal or ExpressionType.NotEqual
          or ExpressionType.LessThan or ExpressionType.LessThanOrEqual
          or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual;

    private ColumnInfo TryGetColumn(Expression e)
    {
        // Unwrap implicit Convert nodes (e.g. enum -> int) to get to the underlying member.
        while (e is UnaryExpression u && u.NodeType == ExpressionType.Convert)
            e = u.Operand;
        if (e is MemberExpression m && m.Expression is ParameterExpression)
            return metadata.Columns.FirstOrDefault(c => c.PropertyName == m.Member.Name);
        return null;
    }

    private static bool IsNullConstant(Expression expr) =>
        expr is ConstantExpression c && c.Value == null;

    private static object EvaluateExpression(Expression expr)
    {
        var lambda = Expression.Lambda(expr);
        var compiled = lambda.Compile();
        return compiled.DynamicInvoke();
    }
}
