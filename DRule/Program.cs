// See https://aka.ms/new-console-template for more information

using DRule;

next:
var input = Console.ReadLine();
if (input == "q") goto break1;

var rule = RuleBuilder.Create<Person>(input);
var q = new Neo4JExpressionVisitor().GetQuery(rule.Filter);
Console.WriteLine($"query is: {q}");

goto next;
break1:
Console.WriteLine("end");

public class Person
{
    public string Name { get; set; }
}