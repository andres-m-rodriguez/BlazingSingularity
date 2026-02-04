using Xunit;

namespace BlazingSingularity.SourceGenerators.Tests;

public class RazorRelayCommandGeneratorTests
{
    [Fact]
    public void RazorCodeBlock_GeneratesCommandsFromRelayCommandMethods()
    {
        var razorContent = """
            @using BlazingSingularity.Commands
            @using System.Threading.Tasks

            <h1>My Page</h1>

            @code {
                [RelayCommand]
                public async Task LoadDataAsync()
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestHelper.RunRazorRelayCommandGenerator(
            razorContent,
            "/TestAssembly/Pages/MyPage.razor");

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("AsyncRelayCommand", generatedSource);
        Assert.Contains("LoadDataAsyncCommand", generatedSource);
        Assert.Contains("partial class MyPage", generatedSource);
    }

    [Fact]
    public void RazorCodeBlock_NoRelayCommandMethods_NoSourceGenerated()
    {
        var razorContent = """
            <h1>My Page</h1>

            @code {
                private string _name = "test";
            }
            """;

        var result = GeneratorTestHelper.RunRazorRelayCommandGenerator(
            razorContent,
            "/TestAssembly/Pages/SimplePage.razor");

        // No [RelayCommand] methods, so no source should be generated
        Assert.Empty(result.GeneratedTrees);
    }

    [Fact]
    public void RazorCodeBlock_MultipleRelayCommandMethods_AllGenerated()
    {
        var razorContent = """
            @using BlazingSingularity.Commands
            @using System.Threading.Tasks

            <h1>My Page</h1>

            @code {
                [RelayCommand]
                public async Task LoadAsync()
                {
                    await Task.CompletedTask;
                }

                [RelayCommand]
                public async Task SaveAsync()
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestHelper.RunRazorRelayCommandGenerator(
            razorContent,
            "/TestAssembly/Pages/MultiPage.razor");

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("LoadAsyncCommand", generatedSource);
        Assert.Contains("SaveAsyncCommand", generatedSource);
    }

    [Fact]
    public void RazorFile_WithNoCodeBlock_NoSourceGenerated()
    {
        var razorContent = """
            <h1>Hello World</h1>
            <p>No code block here.</p>
            """;

        var result = GeneratorTestHelper.RunRazorRelayCommandGenerator(
            razorContent,
            "/TestAssembly/Pages/NoCode.razor");

        Assert.Empty(result.GeneratedTrees);
    }
}
