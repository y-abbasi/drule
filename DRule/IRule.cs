using System.Linq.Expressions;

namespace DRule;

public interface IRule
{
    Expression<Func<Person, bool>> Filter { get; }
}