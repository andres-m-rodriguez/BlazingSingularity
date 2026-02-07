using BlazingSingularity.Commands;
using Xunit;

namespace BlazingSingularity.Tests;

public class AsyncRelayCommandTryExecuteTests
{
    [Fact]
    public async Task TryExecuteAsync_ReturnsSuccess_WhenCommandExecutesSuccessfully()
    {
        var executed = false;
        var command = new AsyncRelayCommand(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        var result = await command.TryExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.True(executed);
    }

    [Fact]
    public async Task TryExecuteAsync_ReturnsFailure_WhenCommandThrows()
    {
        var expectedException = new InvalidOperationException("Test error");
        var command = new AsyncRelayCommand(async () =>
        {
            await Task.Delay(1);
            throw expectedException;
        });

        var result = await command.TryExecuteAsync();

        Assert.True(result.IsFailure);
        Assert.Same(expectedException, result.Exception);
    }

    [Fact]
    public async Task TryExecuteAsync_ReturnsFailure_WhenCanExecuteReturnsFalse()
    {
        var command = new AsyncRelayCommand(
            execute: () => Task.CompletedTask,
            canExecute: () => false
        );

        var result = await command.TryExecuteAsync();

        Assert.True(result.IsFailure);
        Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Equal("Command cannot execute.", result.ErrorMessage);
    }

    [Fact]
    public async Task TryExecuteAsync_SetsIsLoading_DuringExecution()
    {
        var tcs = new TaskCompletionSource();
        var wasLoadingDuringExecution = false;

        var command = new AsyncRelayCommand(async () =>
        {
            wasLoadingDuringExecution = true;
            await tcs.Task;
        });

        var executeTask = command.TryExecuteAsync();
        Assert.True(command.IsLoading);

        tcs.SetResult();
        await executeTask;

        Assert.False(command.IsLoading);
        Assert.True(wasLoadingDuringExecution);
    }

    [Fact]
    public async Task TryExecuteAsync_ResetsIsLoading_WhenExceptionThrown()
    {
        var command = new AsyncRelayCommand(() => throw new Exception("Test"));

        var result = await command.TryExecuteAsync();

        Assert.False(command.IsLoading);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task TryExecuteAsync_ReturnsFailure_WhenOperationCancelled()
    {
        var command = new AsyncRelayCommand(async ct =>
        {
            await Task.Delay(100, ct);
        });

        var executeTask = command.TryExecuteAsync();
        command.CancelCommand();
        var result = await executeTask;

        Assert.True(result.IsFailure);
        Assert.IsType<TaskCanceledException>(result.Exception);
    }
}

public class AsyncRelayCommandGenericTryExecuteTests
{
    [Fact]
    public async Task TryExecuteAsync_ReturnsSuccess_WhenCommandExecutesSuccessfully()
    {
        var capturedValue = 0;
        var command = new AsyncRelayCommand<int>(async value =>
        {
            await Task.Delay(1);
            capturedValue = value;
        });

        var result = await command.TryExecuteAsync(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, capturedValue);
    }

    [Fact]
    public async Task TryExecuteAsync_ReturnsFailure_WhenCommandThrows()
    {
        var expectedException = new ArgumentException("Invalid value");
        var command = new AsyncRelayCommand<int>(async value =>
        {
            await Task.Delay(1);
            throw expectedException;
        });

        var result = await command.TryExecuteAsync(42);

        Assert.True(result.IsFailure);
        Assert.Same(expectedException, result.Exception);
    }

    [Fact]
    public async Task TryExecuteAsync_ReturnsFailure_WhenCanExecuteReturnsFalse()
    {
        var command = new AsyncRelayCommand<int>(
            execute: _ => Task.CompletedTask,
            canExecute: _ => false
        );

        var result = await command.TryExecuteAsync(42);

        Assert.True(result.IsFailure);
        Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Equal("Command cannot execute.", result.ErrorMessage);
    }

    [Fact]
    public async Task TryExecuteAsync_WithObjectParameter_DelegatesToTypedMethod()
    {
        var capturedValue = 0;
        var command = new AsyncRelayCommand<int>(async value =>
        {
            await Task.Delay(1);
            capturedValue = value;
        });

        var result = await command.TryExecuteAsync((object?)42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, capturedValue);
    }

    [Fact]
    public async Task TryExecuteAsync_SetsIsLoading_DuringExecution()
    {
        var tcs = new TaskCompletionSource();

        var command = new AsyncRelayCommand<int>(async _ =>
        {
            await tcs.Task;
        });

        var executeTask = command.TryExecuteAsync(1);
        Assert.True(command.IsLoading);

        tcs.SetResult();
        await executeTask;

        Assert.False(command.IsLoading);
    }

    [Fact]
    public async Task TryExecuteAsync_WithCancellation_ReturnsFailure_WhenCancelled()
    {
        var command = new AsyncRelayCommand<int>(async (_, ct) =>
        {
            await Task.Delay(100, ct);
        });

        var executeTask = command.TryExecuteAsync(1);
        command.CancelCommand();
        var result = await executeTask;

        Assert.True(result.IsFailure);
        Assert.IsType<TaskCanceledException>(result.Exception);
    }
}
