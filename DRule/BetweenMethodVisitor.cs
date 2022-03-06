using System.Linq.Expressions;

namespace DRule;

public class BetweenMethodVisitor : IMethodVisitor
{
    public void Visit(IExpressionVisitor visitor, params Expression[] arguments)
    {
        visitor.Visit(arguments[0]);
        visitor.AppendToResult(" between [");
        visitor.Visit(arguments[1]);
        visitor.AppendToResult(",");
        visitor.Visit(arguments[2]);
        visitor.AppendToResult("]");
    }
}