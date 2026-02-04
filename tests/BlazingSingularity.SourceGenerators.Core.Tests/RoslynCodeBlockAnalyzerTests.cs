using BlazingSingularity.SourceGenerators.RazorAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace BlazingSingularity.SourceGenerators.Core.Tests;

public class RoslynCodeBlockAnalyzerTests
{
    private readonly RoslynCodeBlockAnalyzer _analyzer = new();

    private static Compilation CreateBaseCompilation() =>
        CSharpCompilation.Create(
            "TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static SyntheticClass BuildSyntheticClass(string classSource) =>
        new(
            SourceCode: classSource,
            HeaderLineCount: 0,
            CodeStartLine: 1,
            TotalLines: classSource.Split('\n').Length);

    [Fact]
    public void Analyze_ValidCode_ReturnsSuccess()
    {
        var source = """
            public partial class Counter : object
            {
                private int _count;
                public void Increment() { _count++; }
            }
            """;
        var syntheticClass = BuildSyntheticClass(source);

        var result = _analyzer.Analyze(syntheticClass, CreateBaseCompilation());

        Assert.True(result.Success);
        Assert.NotNull(result.ClassDeclaration);
        Assert.NotNull(result.SemanticModel);
    }

    [Fact]
    public void Analyze_ParseErrors_ReturnsFailure()
    {
        var source = "public class { invalid syntax %%% }}}";
        var syntheticClass = BuildSyntheticClass(source);

        var result = _analyzer.Analyze(syntheticClass, CreateBaseCompilation());

        Assert.False(result.Success);
        Assert.NotEmpty(result.ParseDiagnostics);
    }

    [Fact]
    public void Analyze_NoClassDeclaration_ReturnsFailure()
    {
        var source = "using System;";
        var syntheticClass = BuildSyntheticClass(source);

        var result = _analyzer.Analyze(syntheticClass, CreateBaseCompilation());

        Assert.False(result.Success);
        Assert.Null(result.ClassDeclaration);
    }
}
