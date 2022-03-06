using System.Linq.Expressions;

namespace DRule;

public interface IRule<T>
{
    Expression<Func<T, bool>> Filter { get; }
}