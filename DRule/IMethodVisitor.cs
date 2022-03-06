using System.Linq.Expressions;

namespace DRule;

public interface IMethodVisitor
{
    void Visit(IExpressionVisitor visitor, params Expression[] arguments);
}