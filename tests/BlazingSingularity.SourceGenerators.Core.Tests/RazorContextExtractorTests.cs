using BlazingSingularity.SourceGenerators.RazorAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace BlazingSingularity.SourceGenerators.Core.Tests;

public class RazorContextExtractorTests
{
    private readonly RazorContextExtractor _extractor = new();

    private static Compilation CreateCompilation(string assemblyName = "TestApp") =>
        CSharpCompilation.Create(
            assemblyName,
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    [Fact]
    public void ExtractContext_IncludesCommonBlazorUsings()
    {
        var razor = "<h1>Hello</h1>";
        var compilation = CreateCompilation();

        var context = _extractor.ExtractContext(razor, "TestApp/Components/Counter.razor", compilation);

        Assert.Contains("System", context.UsingDirectives);
        Assert.Contains("System.Threading.Tasks", context.UsingDirectives);
        Assert.Contains("Microsoft.AspNetCore.Components", context.UsingDirectives);
    }

    [Fact]
    public void ExtractContext_ExplicitNamespaceDirective_UsesIt()
    {
        var razor = "@namespace MyApp.Pages\n<h1>Hello</h1>";
        var compilation = CreateCompilation();

        var context = _extractor.ExtractContext(razor, "TestApp/Counter.razor", compilation);

        Assert.Equal("MyApp.Pages", context.Namespace);
    }

    [Fact]
    public void ExtractContext_InheritsDirective_ExtractsBaseClass()
    {
        var razor = "@inherits CustomBase\n<h1>Hello</h1>";
        var compilation = CreateCompilation();

        var context = _extractor.ExtractContext(razor, "TestApp/Counter.razor", compilation);

        Assert.Equal("CustomBase", context.InheritsFrom);
    }
}
