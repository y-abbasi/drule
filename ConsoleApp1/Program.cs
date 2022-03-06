// See https://aka.ms/new-console-template for more information

using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ConsoleApp1;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

var defaultNamespaces =
    new[]
    {
        "System",
        "System.IO",
        "System.Net",
        "System.Linq",
        "System.Linq.Expressions",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Collections.Generic"
    };

var runtimePath = @"/usr/local/share/dotnet/shared/Microsoft.NETCore.App/6.0.1/{0}.dll";

var defaultReferences =
    new[]
    {
        MetadataReference.CreateFromFile(string.Format(runtimePath, "System")),
        MetadataReference.CreateFromFile(string.Format(runtimePath, "System.Core")),
        MetadataReference.CreateFromFile(string.Format(runtimePath, "System.Runtime")),
        MetadataReference.CreateFromFile(string.Format(runtimePath, "System.Collections")),
        MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Person).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Func<,>).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Expression<>).GetTypeInfo().Assembly.Location)
    };

var defaultCompilationOptions =
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        .WithOverflowChecks(true).WithOptimizationLevel(OptimizationLevel.Release)
        .WithUsings(defaultNamespaces);

SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null)
{
    var stringText = SourceText.From(text, Encoding.UTF8);
    return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
}


var source = @"
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

public class Test : IRule{
    public Expression<System.Func<Person, bool>> Filter{
        get{
            return ##src##;
            //return p => p.FirstName == ""yaser"" && p.Addresses.Any(a => a.City == p.LastName);
            //return p => p.FirstName == ""yaser"" && new []{""abbasi"", ""ahmadi""}.Contains(p.LastName);
            //return p => p.FirstName == ""yaser"" && new List<string>{""abbasi"", ""ahmadi""}.Any(y => y == p.LastName);
        }
    }
}";

next:
var input = Console.ReadLine();
if(input == "q") goto break1;
var parsedSyntaxTree = Parse(source.Replace("##src##", input), "", CSharpParseOptions.Default
    .WithLanguageVersion(LanguageVersion.CSharp10));

var compilation
    = CSharpCompilation.Create("Test.dll", new[] {parsedSyntaxTree},
        defaultReferences,
        defaultCompilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
try
{
    using var exe = new MemoryStream();
    var result = compilation.Emit(exe);
    if (result.Success == false)
    {
        foreach (var err in result.Diagnostics)
        {
            Console.WriteLine(err);
        }
    }
    else
    {
        var ass = Assembly.Load(exe.GetBuffer());
        var rule = (IRule) Activator.CreateInstance(ass.GetExportedTypes()[0])!;
        var q = new Neo4JExpressionVisitor().GetQuery(rule.Filter);
        Console.WriteLine(result.Success ? "Sucess!!" : "Failed");
        Console.WriteLine($"query is: {q}");
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}
goto next;
break1:
Console.WriteLine("end");
namespace ConsoleApp1
{
    public class V : CSharpSyntaxRewriter
    {
        private Stack<Func<int>> result = new();
        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            Visit(node.Left);
            Visit(node.Right);
            var left = result.Pop();
            var right  = result.Pop();
            switch (node.OperatorToken.ValueText)
            {
                case "+": result.Push(() => left() + right()); break;
                case "*": result.Push(() => left() * right()); break;
                case "-": result.Push(() => left() - right()); break;
                case "/": result.Push(() => left() / right()); break;
            }
            return node;
        }

        public Func<int> Evaluator(SyntaxNode? node)
        {
            Visit(node);
            return result.Pop();
        }
    
        public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            result.Push(() => Convert.ToInt32(node.Token.Text));
            return base.VisitLiteralExpression(node);
        }

        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            return base.VisitIfStatement(node);
        }

        public override SyntaxNode? VisitElseClause(ElseClauseSyntax node)
        {
            return base.VisitElseClause(node);
        }

        public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            return base.VisitParenthesizedLambdaExpression(node);
        }

        public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            return base.VisitSimpleLambdaExpression(node);
        }

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            return base.Visit(node);
        }
    }
}