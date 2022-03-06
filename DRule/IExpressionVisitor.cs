using System.Linq.Expressions;

namespace DRule;

public interface IExpressionVisitor
{
    Expression Visit(Expression node);
    internal void AppendToResult(string text);
}