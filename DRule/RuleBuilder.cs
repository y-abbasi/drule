using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace DRule;

public class RuleBuilder
{
    public static IRule<T> Create<T>(string rule)
    {
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
                MetadataReference.CreateFromFile(typeof(T).GetTypeInfo().Assembly.Location),
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
using DRule;

public class Test : IRule<TTemplate>{
    public Expression<System.Func<TTemplate, bool>> Filter{
        get{
            return ##src##;
        }
    }
}";
        var parsedSyntaxTree = Parse(source
            .Replace("TTemplate", typeof(T).FullName)
            .Replace("##src##", rule), "", CSharpParseOptions.Default
            .WithLanguageVersion(LanguageVersion.CSharp10));

        var compilation
            = CSharpCompilation.Create("Test.dll", new[] {parsedSyntaxTree},
                defaultReferences,
                defaultCompilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
        using var exe = new MemoryStream();
        var result = compilation.Emit(exe);
        if (result.Success == false)
        {
            throw new RuleCompileException(result.Diagnostics);
        }
        else
        {
            var ass = Assembly.Load(exe.GetBuffer());
            return (IRule<T>) Activator.CreateInstance(ass.GetExportedTypes()[0])!;
        }
    }
}

public class RuleCompileException : Exception
{
    public ImmutableArray<Diagnostic> ResultDiagnostics { get; }

    public RuleCompileException(ImmutableArray<Diagnostic> resultDiagnostics)
    {
        ResultDiagnostics = resultDiagnostics;
    }
}