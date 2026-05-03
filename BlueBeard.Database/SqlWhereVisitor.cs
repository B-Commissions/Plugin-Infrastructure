using System;
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
            _sb.Append($"`{columnName}`");
            return node;
        }
        AddParameter(EvaluateExpression(node));
        return node;
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
