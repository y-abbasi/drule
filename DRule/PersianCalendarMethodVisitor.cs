using System.Linq.Expressions;

namespace DRule;

public class PersianCalendarMethodVisitor : IMethodVisitor
{
    public void Visit(IExpressionVisitor visitor, params Expression[] arguments)
    {
        visitor.Visit(Expression.Constant(Helper.PDate((string) arguments.Cast<ConstantExpression>().First().Value)
            .ToString("yyyy/MM/dd")));
    }
}