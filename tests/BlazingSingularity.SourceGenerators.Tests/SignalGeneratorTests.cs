using Microsoft.CodeAnalysis;
using Xunit;

namespace BlazingSingularity.SourceGenerators.Tests;

public class SignalGeneratorTests
{
    [Fact]
    public void IntField_ProducesSignalPropertyAndAccessor()
    {
        var source = """
            using BlazingSingularity.Signals;

            namespace TestApp;

            public partial class MyViewModel
            {
                [Signal]
                private int _count;
            }
            """;

        var result = GeneratorTestHelper.RunSignalGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("Signal<int>", generatedSource);
        Assert.Contains("_countSignal", generatedSource);
        Assert.Contains("public int Count", generatedSource);
        Assert.Contains("CountSignal", generatedSource);
        Assert.Contains("_countSignal.Notify(value)", generatedSource);
    }

    [Fact]
    public void StringField_ProducesSignalPropertyAndAccessor()
    {
        var source = """
            using BlazingSingularity.Signals;

            namespace TestApp;

            public partial class MyViewModel
            {
                [Signal]
                private string? _name;
            }
            """;

        var result = GeneratorTestHelper.RunSignalGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("Signal<string?>", generatedSource);
        Assert.Contains("_nameSignal", generatedSource);
        Assert.Contains("public string? Name", generatedSource);
        Assert.Contains("NameSignal", generatedSource);
    }

    [Fact]
    public void MultipleFields_ProducesMultipleSignals()
    {
        var source = """
            using BlazingSingularity.Signals;

            namespace TestApp;

            public partial class MyViewModel
            {
                [Signal]
                private int _count;

                [Signal]
                private string? _name;
            }
            """;

        var result = GeneratorTestHelper.RunSignalGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("CountSignal", generatedSource);
        Assert.Contains("NameSignal", generatedSource);
        Assert.Contains("_countSignal", generatedSource);
        Assert.Contains("_nameSignal", generatedSource);
    }

    [Fact]
    public void NonPartialClass_ReportsBLAZING003()
    {
        var source = """
            using BlazingSingularity.Signals;

            namespace TestApp;

            public class MyViewModel
            {
                [Signal]
                private int _count;
            }
            """;

        var result = GeneratorTestHelper.RunSignalGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BLAZING003", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("MyViewModel", diagnostic.GetMessage());
    }

    [Fact]
    public void GeneratedProperty_IncludesEqualityCheck()
    {
        var source = """
            using BlazingSingularity.Signals;

            namespace TestApp;

            public partial class MyViewModel
            {
                [Signal]
                private int _value;
            }
            """;

        var result = GeneratorTestHelper.RunSignalGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("EqualityComparer<int>.Default.Equals(_value, value)", generatedSource);
    }

    [Fact]
    public void FieldWithoutUnderscore_ProducesCorrectPropertyName()
    {
        var source = """
            using BlazingSingularity.Signals;

            namespace TestApp;

            public partial class MyViewModel
            {
                [Signal]
                private int count;
            }
            """;

        var result = GeneratorTestHelper.RunSignalGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("public int Count", generatedSource);
        Assert.Contains("CountSignal", generatedSource);
    }
}
