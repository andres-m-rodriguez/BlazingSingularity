using BlazingSingularity.SourceGenerators.RazorAnalysis;
using Xunit;

namespace BlazingSingularity.SourceGenerators.Core.Tests;

public class SyntheticClassBuilderTests
{
    private readonly SyntheticClassBuilder _builder = new();

    private static RazorContext CreateContext(string ns = "TestApp", string className = "Counter") =>
        new(
            UsingDirectives: ["System"],
            Namespace: ns,
            ClassName: className,
            InheritsFrom: "ComponentBase");

    [Fact]
    public void BuildClass_ProducesValidClassStructure()
    {
        var context = CreateContext();

        var result = _builder.BuildClass(context, "private int _count;");

        Assert.Contains("namespace TestApp", result.SourceCode);
        Assert.Contains("public partial class Counter : ComponentBase", result.SourceCode);
        Assert.Contains("private int _count;", result.SourceCode);
    }

    [Fact]
    public void BuildClass_HeaderLineCount_ExcludesCodeContent()
    {
        var context = CreateContext();

        var result = _builder.BuildClass(context, "private int _count;");

        // Header: #nullable, using System, blank line, namespace, {, class decl, {
        Assert.True(result.HeaderLineCount > 0);
        Assert.True(result.CodeStartLine > result.HeaderLineCount);
    }

    [Fact]
    public void MapToRazorLine_MapsCorrectly()
    {
        var context = CreateContext();
        var syntheticClass = _builder.BuildClass(context, "line1\nline2\nline3");
        int razorCodeBlockStart = 10;

        int mappedFirst = SyntheticClassBuilder.MapToRazorLine(syntheticClass, syntheticClass.CodeStartLine, razorCodeBlockStart);
        int mappedSecond = SyntheticClassBuilder.MapToRazorLine(syntheticClass, syntheticClass.CodeStartLine + 1, razorCodeBlockStart);

        Assert.Equal(10, mappedFirst);
        Assert.Equal(11, mappedSecond);
    }
}
