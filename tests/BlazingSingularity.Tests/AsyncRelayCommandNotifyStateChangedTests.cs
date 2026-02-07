using BlazingSingularity.Commands;
using Xunit;

namespace BlazingSingularity.Tests;

public class AsyncRelayCommandNotifyStateChangedTests
{
    [Fact]
    public async Task ExecuteAsync_CallsNotifyStateChanged_AfterExecution()
    {
        var executed = false;
        var notified = false;
        var command = new AsyncRelayCommand(
            execute: async () =>
            {
                await Task.Delay(1);
                executed = true;
            },
            canExecute: null,
            notifyStateChanged: () =>
            {
                notified = true;
                return Task.CompletedTask;
            }
        );

        await command.ExecuteAsync();

        Assert.True(executed);
        Assert.True(notified);
    }

    [Fact]
    public async Task ExecuteAsync_CallsNotifyStateChanged_EvenWhenExceptionThrown()
    {
        var notified = false;
        var command = new AsyncRelayCommand(
            execute: () => throw new InvalidOperationException("Test error"),
            canExecute: null,
            notifyStateChanged: () =>
            {
                notified = true;
                return Task.CompletedTask;
            }
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteAsync());
        Assert.True(notified);
    }

    [Fact]
    public async Task TryExecuteAsync_CallsNotifyStateChanged_AfterExecution()
    {
        var executed = false;
        var notified = false;
        var command = new AsyncRelayCommand(
            execute: async () =>
            {
                await Task.Delay(1);
                executed = true;
            },
            canExecute: null,
            notifyStateChanged: () =>
            {
                notified = true;
                return Task.CompletedTask;
            }
        );

        var result = await command.TryExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.True(executed);
        Assert.True(notified);
    }

    [Fact]
    public async Task TryExecuteAsync_CallsNotifyStateChanged_EvenWhenExceptionThrown()
    {
        var notified = false;
        var command = new AsyncRelayCommand(
            execute: () => throw new InvalidOperationException("Test error"),
            canExecute: null,
            notifyStateChanged: () =>
            {
                notified = true;
                return Task.CompletedTask;
            }
        );

        var result = await command.TryExecuteAsync();

        Assert.True(result.IsFailure);
        Assert.True(notified);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotThrow_WhenNotifyStateChangedIsNull()
    {
        var executed = false;
        var command = new AsyncRelayCommand(
            execute: async () =>
            {
                await Task.Delay(1);
                executed = true;
            },
            canExecute: null,
            notifyStateChanged: null
        );

        await command.ExecuteAsync();

        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_CallsNotifyStateChanged()
    {
        var notified = false;
        var command = new AsyncRelayCommand(
            execute: async ct =>
            {
                await Task.Delay(1, ct);
            },
            canExecute: null,
            notifyStateChanged: () =>
            {
                notified = true;
                return Task.CompletedTask;
            }
        );

        await command.ExecuteAsync();

        Assert.True(notified);
    }
}

public class AsyncRelayCommandGenericNotifyStateChangedTests
{
    [Fact]
    public async Task ExecuteAsync_CallsNotifyStateChanged_AfterExecution()
    {
        var capturedValue = 0;
        var notified = false;
        var command = new AsyncRelayCommand<int>(
            execute: async value =>
            {
                await Task.Delay(1);
                capturedValue = value;
            },
            canExecute: null,
            notifyStateChanged: () =>
            {
                notified = true;
                return Task.CompletedTask;
            }
        );

        await command.ExecuteAsync(42);

        Assert.Equal(42, capturedValue);
        Assert.True(notified);
    }

    [Fact]
    public async Task TryExecuteAsync_CallsNotifyStateChanged_AfterExecution()
    {
        var capturedValue = 0;
        var notified = false;
        var command = new AsyncRelayCommand<int>(
            execute: async value =>
            {
                await Task.Delay(1);
                capturedValue = value;
            },
            canExecute: null,
            notifyStateChanged: () =>
            {
                notified = true;
                return Task.CompletedTask;
            }
        );

        var result = await command.TryExecuteAsync(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, capturedValue);
        Assert.True(notified);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_CallsNotifyStateChanged()
    {
        var notified = false;
        var command = new AsyncRelayCommand<int>(
            execute: async (value, ct) =>
            {
                await Task.Delay(1, ct);
            },
            canExecute: null,
            notifyStateChanged: () =>
            {
                notified = true;
                return Task.CompletedTask;
            }
        );

        await command.ExecuteAsync(42);

        Assert.True(notified);
    }
}
