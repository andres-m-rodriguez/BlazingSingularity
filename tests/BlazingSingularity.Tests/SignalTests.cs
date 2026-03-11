using BlazingSingularity.Signals;
using Xunit;

namespace BlazingSingularity.Tests;

public class SignalTests
{
    [Fact]
    public void OnChange_CallbackIsFired_WhenNotifyIsCalled()
    {
        // Arrange
        var signal = new Signal<string>();
        string? receivedValue = null;
        signal.OnChange(value => receivedValue = value);

        // Act
        signal.Notify("test");

        // Assert
        Assert.Equal("test", receivedValue);
    }

    [Fact]
    public void OnChange_MultipleCallbacksAreFired_WhenNotifyIsCalled()
    {
        // Arrange
        var signal = new Signal<int>();
        int callback1Count = 0;
        int callback2Count = 0;
        signal.OnChange(_ => callback1Count++);
        signal.OnChange(_ => callback2Count++);

        // Act
        signal.Notify(42);

        // Assert
        Assert.Equal(1, callback1Count);
        Assert.Equal(1, callback2Count);
    }

    [Fact]
    public void OnChange_ActionOverload_CallbackIsFired()
    {
        // Arrange
        var signal = new Signal<string>();
        bool wasCalled = false;
        signal.OnChange(() => wasCalled = true);

        // Act
        signal.Notify("test");

        // Assert
        Assert.True(wasCalled);
    }

    [Fact]
    public void Notify_NoCallbacks_DoesNotThrow()
    {
        // Arrange
        var signal = new Signal<string>();

        // Act & Assert - should not throw
        signal.Notify("test");
    }
}
