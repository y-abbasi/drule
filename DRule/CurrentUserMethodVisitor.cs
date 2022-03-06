using System.Linq.Expressions;

namespace DRule;

internal class CurrentUserMethodVisitor : IMethodVisitor
{
    public void Visit(IExpressionVisitor visitor, params Expression[] arguments) => Helper.CurrentUser();
}