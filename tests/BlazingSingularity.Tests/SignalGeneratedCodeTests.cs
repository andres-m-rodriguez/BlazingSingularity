using System.Collections.Generic;
using BlazingSingularity.Signals;
using Xunit;

namespace BlazingSingularity.Tests;

/// <summary>
/// Tests that simulate the pattern used by generated signal code
/// </summary>
public class SignalGeneratedCodeTests
{
    /// <summary>
    /// Simulates a component with a [Signal] field, matching the generated code pattern
    /// </summary>
    private class SimulatedComponent
    {
        // Generated signal backing field (value is now stored in the signal itself)
        private readonly Signal<string?> _searchTextSignal = new();

        // Generated property delegates to signal's Value
        public string? SearchText
        {
            get => _searchTextSignal.Value;
            set => _searchTextSignal.Value = value;
        }

        // Generated signal property
        public Signal<string?> SearchTextSignal => _searchTextSignal;
    }

    [Fact]
    public void GeneratedPattern_CallbackFires_WhenPropertyIsSet()
    {
        // Arrange
        var component = new SimulatedComponent();
        string? receivedValue = null;
        component.SearchTextSignal.OnChange(value => receivedValue = value);

        // Act - simulate what @bind does
        component.SearchText = "hello";

        // Assert
        Assert.Equal("hello", receivedValue);
    }

    [Fact]
    public void GeneratedPattern_CallbackDoesNotFire_WhenSameValueIsSet()
    {
        // Arrange
        var component = new SimulatedComponent();
        int callbackCount = 0;
        component.SearchText = "initial";
        component.SearchTextSignal.OnChange(_ => callbackCount++);

        // Act - set same value
        component.SearchText = "initial";

        // Assert - callback should not fire
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public void GeneratedPattern_CallbackFires_WhenValueChanges()
    {
        // Arrange
        var component = new SimulatedComponent();
        int callbackCount = 0;
        component.SearchText = "initial";
        component.SearchTextSignal.OnChange(_ => callbackCount++);

        // Act - set different value
        component.SearchText = "changed";

        // Assert
        Assert.Equal(1, callbackCount);
    }

    [Fact]
    public void GeneratedPattern_SameSignalInstance_UsedByPropertyAndNotify()
    {
        // Arrange
        var component = new SimulatedComponent();

        // Get the signal instance
        var signalFromProperty = component.SearchTextSignal;

        // Register callback
        bool callbackFired = false;
        signalFromProperty.OnChange(_ => callbackFired = true);

        // Act - set property (which calls Notify on internal signal)
        component.SearchText = "test";

        // Assert - callback should fire because it's the same instance
        Assert.True(callbackFired);
    }
}
