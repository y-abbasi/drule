using System.Globalization;
using System.Linq.Expressions;

namespace ConsoleApp1;

public class Neo4JMethodAttribute : Attribute
{
    public Type Convertor { get; set; }

    public Neo4JMethodAttribute(Type convertor)
    {
        Convertor = convertor;
    }
}

public static class Helper
{
    [Neo4JMethod(typeof(PersianCalendarMethodVisitor))]
    public static DateTime PDate(string persianDate)
    {
        var parts = persianDate.Split('/');
        var pd = new PersianCalendar();
        return new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), pd);
    }

    [Neo4JMethod(typeof(CurrentUserMethodVisitor))]
    public static int CurrentUser() => 10;

    [Neo4JMethod(typeof(BetweenMethodVisitor))]
    public static bool Between<T>(this T date, T from, T to) where T : IComparable<T>
    {
        return from.CompareTo(date) <= 0 && date.CompareTo(to) <= 0;
    }
}

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

internal class CurrentUserMethodVisitor : IMethodVisitor
{
    public void Visit(IExpressionVisitor visitor, params Expression[] arguments) => Helper.CurrentUser();
}

public class PersianCalendarMethodVisitor : IMethodVisitor
{
    public void Visit(IExpressionVisitor visitor, params Expression[] arguments)
    {
        visitor.Visit(Expression.Constant(Helper.PDate((string) arguments.Cast<ConstantExpression>().First().Value)
            .ToString("yyyy/MM/dd")));
    }
}

public interface IMethodVisitor
{
    void Visit(IExpressionVisitor visitor, params Expression[] arguments);
}

public interface IRule
{
    Expression<Func<Person, bool>> Filter { get; }
}

public class Person
{
    public string FirstName { get; set; }
    public int CurrentUserId() => 200;
    public int UserId { get; set; }
    public string LastName { get; set; }
    public List<Address> Addresses { get; set; }
    public DateTime BirthDate { get; set; }
    public int Grade { get; set; }
}

public class Address
{
    public string City { get; set; }
}