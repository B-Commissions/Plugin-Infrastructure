using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace BlueBeard.Database;

public class SqlWhereVisitor : ExpressionVisitor
{
    private readonly StringBuilder _sb = new();
    private readonly List<object> _parameters = new();
    private readonly TableMetadata _metadata;

    public string Sql => _sb.ToString();
    public List<object> Parameters => _parameters;

    public SqlWhereVisitor(TableMetadata metadata) { _metadata = metadata; }

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
        Visit(node.Left);
        switch (node.NodeType)
        {
            case ExpressionType.Equal:
                if (IsNullConstant(node.Right)) { _sb.Append(" IS NULL)"); return node; }
                _sb.Append(" = "); break;
            case ExpressionType.NotEqual:
                if (IsNullConstant(node.Right)) { _sb.Append(" IS NOT NULL)"); return node; }
                _sb.Append(" != "); break;
            case ExpressionType.LessThan: _sb.Append(" < "); break;
            case ExpressionType.GreaterThan: _sb.Append(" > "); break;
            case ExpressionType.LessThanOrEqual: _sb.Append(" <= "); break;
            case ExpressionType.GreaterThanOrEqual: _sb.Append(" >= "); break;
            case ExpressionType.AndAlso: _sb.Append(" AND "); break;
            case ExpressionType.OrElse: _sb.Append(" OR "); break;
            default: throw new NotSupportedException($"Binary operator '{node.NodeType}' is not supported.");
        }
        Visit(node.Right);
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ParameterExpression)
        {
            var columnName = _metadata.GetColumnName(node.Member.Name);
            _sb.Append($"`{columnName}`");
            return node;
        }
        var value = EvaluateExpression(node);
        AddParameter(value);
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
        _sb.Append($"@p{_parameters.Count}");
        _parameters.Add(value);
    }

    private static bool IsNullConstant(Expression expr) => expr is ConstantExpression c && c.Value == null;

    private static object EvaluateExpression(Expression expr)
    {
        var lambda = Expression.Lambda(expr);
        var compiled = lambda.Compile();
        return compiled.DynamicInvoke();
    }
}
