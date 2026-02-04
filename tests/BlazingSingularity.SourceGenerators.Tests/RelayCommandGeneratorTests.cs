using Microsoft.CodeAnalysis;
using Xunit;

namespace BlazingSingularity.SourceGenerators.Tests;

public class RelayCommandGeneratorTests
{
    [Fact]
    public void AsyncTaskMethod_ProducesAsyncRelayCommand()
    {
        var source = """
            using System.Threading.Tasks;
            using BlazingSingularity.Commands;

            namespace TestApp;

            public partial class MyViewModel
            {
                [RelayCommand]
                public async Task LoadDataAsync()
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestHelper.RunRelayCommandGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("AsyncRelayCommand", generatedSource);
        Assert.Contains("LoadDataAsyncCommand", generatedSource);
        Assert.Contains("new(LoadDataAsync)", generatedSource);
    }

    [Fact]
    public void SyncVoidMethod_ProducesAsyncRelayCommandWithTaskWrapper()
    {
        var source = """
            using BlazingSingularity.Commands;

            namespace TestApp;

            public partial class MyViewModel
            {
                [RelayCommand]
                public void DoWork()
                {
                }
            }
            """;

        var result = GeneratorTestHelper.RunRelayCommandGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("AsyncRelayCommand", generatedSource);
        Assert.Contains("DoWorkCommand", generatedSource);
        Assert.Contains("DoWork()", generatedSource);
        Assert.Contains("Task.CompletedTask", generatedSource);
    }

    [Fact]
    public void CanExecuteMethod_IsAutoDiscovered()
    {
        var source = """
            using System.Threading.Tasks;
            using BlazingSingularity.Commands;

            namespace TestApp;

            public partial class MyViewModel
            {
                [RelayCommand]
                public async Task SaveAsync()
                {
                    await Task.CompletedTask;
                }

                public bool CanSaveAsync()
                {
                    return true;
                }
            }
            """;

        var result = GeneratorTestHelper.RunRelayCommandGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("SaveAsyncCommand", generatedSource);
        Assert.Contains("new(SaveAsync, CanSaveAsync)", generatedSource);
    }

    [Fact]
    public void CancellationTokenParameter_IsDetected()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            using BlazingSingularity.Commands;

            namespace TestApp;

            public partial class MyViewModel
            {
                [RelayCommand]
                public async Task LoadAsync(CancellationToken cancellationToken)
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestHelper.RunRelayCommandGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        // CancellationToken is filtered out, so this should produce AsyncRelayCommand (not generic)
        Assert.Contains("private AsyncRelayCommand?", generatedSource);
        Assert.Contains("public AsyncRelayCommand LoadAsyncCommand", generatedSource);
    }

    [Fact]
    public void SingleParameter_ProducesGenericAsyncRelayCommand()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            using BlazingSingularity.Commands;

            namespace TestApp;

            public partial class MyViewModel
            {
                [RelayCommand]
                public async Task DeleteItemAsync(Guid id)
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestHelper.RunRelayCommandGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        Assert.Contains("AsyncRelayCommand<System.Guid>", generatedSource);
        Assert.Contains("DeleteItemAsyncCommand", generatedSource);
    }

    [Fact]
    public void NonPartialClass_ReportsBLAZING001()
    {
        var source = """
            using System.Threading.Tasks;
            using BlazingSingularity.Commands;

            namespace TestApp;

            public class MyViewModel
            {
                [RelayCommand]
                public async Task LoadAsync()
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestHelper.RunRelayCommandGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BLAZING001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("MyViewModel", diagnostic.GetMessage());
    }

    [Fact]
    public void MethodWithMoreThan1Parameter_ReportsBLAZING002()
    {
        var source = """
            using System.Threading.Tasks;
            using BlazingSingularity.Commands;

            namespace TestApp;

            public partial class MyViewModel
            {
                [RelayCommand]
                public async Task DoSomethingAsync(int p1, int p2)
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestHelper.RunRelayCommandGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BLAZING002", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("DoSomethingAsync", diagnostic.GetMessage());
    }

    [Fact]
    public void MultipleParameters_ReportsBLAZING002()
    {
        var source = """
            using System.Threading.Tasks;
            using BlazingSingularity.Commands;

            namespace TestApp;

            public partial class MyViewModel
            {
                [RelayCommand]
                public async Task UpdateAsync(string name, int age)
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestHelper.RunRelayCommandGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("BLAZING002", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("UpdateAsync", diagnostic.GetMessage());
    }

    [Fact]
    public void SingleParameterWithCancellationToken_ProducesGenericAsyncRelayCommand()
    {
        var source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using BlazingSingularity.Commands;

            namespace TestApp;

            public partial class MyViewModel
            {
                [RelayCommand]
                public async Task DeleteItemAsync(Guid id, CancellationToken cancellationToken)
                {
                    await Task.CompletedTask;
                }
            }
            """;

        var result = GeneratorTestHelper.RunRelayCommandGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedTrees);
        var generatedSource = generated.GetText().ToString();

        // CancellationToken is filtered out, so only Guid remains as a single parameter
        Assert.Contains("AsyncRelayCommand<System.Guid>", generatedSource);
        Assert.Contains("DeleteItemAsyncCommand", generatedSource);
    }
}
