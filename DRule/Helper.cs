using System.Globalization;

namespace DRule;

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