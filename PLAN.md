# Signal-Based Reactive Commands Implementation Plan

## Goal
Add a reactive/signal system where commands automatically execute when their dependencies change:
```csharp
LoadTodoListAsyncCommand.WithDependency(searchText);
```

## Proposed API Design

### Usage Example
```csharp
@code {
    private Signal<string?> searchText = new();

    protected override void OnInitialized()
    {
        // Command auto-executes when searchText.Value changes
        LoadTodoListAsyncCommand
            .WithDependency(searchText)
            .WithDebounce(300);  // Optional: 300ms debounce
    }

    [RelayCommand]
    public async Task LoadTodoListAsync()
    {
        todos = await TodoHttpClient.GetTodosAsync(searchText.Value);
    }
}

<!-- In markup -->
<input @bind="searchText.Value" @bind:event="oninput" />
```

---

## Implementation Steps

### Step 1: Create `ISignal` and `Signal<T>` Classes

**File:** `BlazingSingularity/Signals/ISignal.cs`
```csharp
namespace BlazingSingularity.Signals;

public interface ISignal
{
    event EventHandler? ValueChanged;
    object? BoxedValue { get; }
}

public interface ISignal<T> : ISignal
{
    T Value { get; set; }
}
```

**File:** `BlazingSingularity/Signals/Signal.cs`
```csharp
namespace BlazingSingularity.Signals;

public class Signal<T> : ISignal<T>
{
    private T _value;

    public Signal(T initialValue = default!)
    {
        _value = initialValue;
    }

    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public object? BoxedValue => Value;
    public event EventHandler? ValueChanged;

    // Implicit conversion for convenience
    public static implicit operator T(Signal<T> signal) => signal.Value;
}
```

### Step 2: Create `ReactiveCommandBinding` Class

**File:** `BlazingSingularity/Signals/ReactiveCommandBinding.cs`

This class manages the connection between signals and commands:
```csharp
namespace BlazingSingularity.Signals;

public class ReactiveCommandBinding : IDisposable
{
    private readonly IAsyncCommand _command;
    private readonly List<ISignal> _signals = new();
    private readonly List<EventHandler> _handlers = new();
    private CancellationTokenSource? _debounceCts;
    private int _debounceMs = 0;
    private bool _cancelPrevious = true;
    private bool _disposed;

    internal ReactiveCommandBinding(IAsyncCommand command)
    {
        _command = command;
    }

    public ReactiveCommandBinding WithDependency(ISignal signal)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ReactiveCommandBinding));

        EventHandler handler = async (s, e) => await OnSignalChanged();
        signal.ValueChanged += handler;
        _signals.Add(signal);
        _handlers.Add(handler);
        return this;
    }

    public ReactiveCommandBinding WithDebounce(int milliseconds)
    {
        _debounceMs = milliseconds;
        return this;
    }

    public ReactiveCommandBinding WithoutCancelPrevious()
    {
        _cancelPrevious = false;
        return this;
    }

    private async Task OnSignalChanged()
    {
        // Cancel previous debounce timer
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();

        try
        {
            if (_debounceMs > 0)
            {
                await Task.Delay(_debounceMs, _debounceCts.Token);
            }

            // Cancel in-flight command if configured
            if (_cancelPrevious && _command.IsLoading)
            {
                _command.CancelCommand();
            }

            await _command.ExecuteAsync(null);
        }
        catch (TaskCanceledException)
        {
            // Debounce was cancelled, ignore
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        for (int i = 0; i < _signals.Count; i++)
        {
            _signals[i].ValueChanged -= _handlers[i];
        }

        _signals.Clear();
        _handlers.Clear();
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
    }
}
```

### Step 3: Create Extension Methods for Commands

**File:** `BlazingSingularity/Signals/ReactiveCommandExtensions.cs`
```csharp
namespace BlazingSingularity.Signals;

public static class ReactiveCommandExtensions
{
    public static ReactiveCommandBinding WithDependency(
        this IAsyncCommand command,
        ISignal signal)
    {
        var binding = new ReactiveCommandBinding(command);
        return binding.WithDependency(signal);
    }

    // For multiple dependencies at once
    public static ReactiveCommandBinding WithDependencies(
        this IAsyncCommand command,
        params ISignal[] signals)
    {
        var binding = new ReactiveCommandBinding(command);
        foreach (var signal in signals)
        {
            binding.WithDependency(signal);
        }
        return binding;
    }
}
```

### Step 4: Add Computed Signals (Optional Enhancement)

**File:** `BlazingSingularity/Signals/ComputedSignal.cs`
```csharp
namespace BlazingSingularity.Signals;

public class ComputedSignal<T> : ISignal<T>, IDisposable
{
    private readonly Func<T> _compute;
    private readonly List<ISignal> _dependencies = new();
    private T _cachedValue;
    private bool _disposed;

    public ComputedSignal(Func<T> compute, params ISignal[] dependencies)
    {
        _compute = compute;
        _cachedValue = compute();

        foreach (var dep in dependencies)
        {
            _dependencies.Add(dep);
            dep.ValueChanged += OnDependencyChanged;
        }
    }

    public T Value
    {
        get => _cachedValue;
        set => throw new InvalidOperationException("Cannot set value of computed signal");
    }

    public object? BoxedValue => Value;
    public event EventHandler? ValueChanged;

    private void OnDependencyChanged(object? sender, EventArgs e)
    {
        var newValue = _compute();
        if (!EqualityComparer<T>.Default.Equals(_cachedValue, newValue))
        {
            _cachedValue = newValue;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var dep in _dependencies)
        {
            dep.ValueChanged -= OnDependencyChanged;
        }
    }
}
```

### Step 5: Update Project Exports

**File:** Update `BlazingSingularity/Commands/` or create new namespace exports

Ensure the new types are accessible:
- `BlazingSingularity.Signals.Signal<T>`
- `BlazingSingularity.Signals.ISignal`
- `BlazingSingularity.Signals.ReactiveCommandExtensions`

---

## File Structure After Implementation

```
BlazingSingularity/
├── Commands/
│   ├── (existing files...)
│
├── Signals/                          (NEW)
│   ├── ISignal.cs
│   ├── Signal.cs
│   ├── ComputedSignal.cs
│   ├── ReactiveCommandBinding.cs
│   └── ReactiveCommandExtensions.cs
```

---

## Updated Todo Page Example

```razor
@page "/todos"
@using BlazingSingularity.Signals
@implements IDisposable

<input @bind="searchText.Value" @bind:event="oninput" />

@foreach (var todo in todos) { ... }

@code {
    private Signal<string?> searchText = new();
    private ReactiveCommandBinding? _binding;

    protected override async Task OnInitializedAsync()
    {
        _binding = LoadTodoListAsyncCommand
            .WithDependency(searchText)
            .WithDebounce(300);

        await LoadTodoListAsyncCommand.ExecuteAsync();
    }

    [RelayCommand]
    public async Task LoadTodoListAsync()
    {
        todos = await TodoHttpClient.GetTodosAsync(searchText.Value);
    }

    public void Dispose() => _binding?.Dispose();
}
```

---

## Implementation Order

1. **ISignal.cs** - Interface definitions
2. **Signal.cs** - Basic signal implementation
3. **ReactiveCommandBinding.cs** - Binding logic with debounce
4. **ReactiveCommandExtensions.cs** - Extension methods
5. **ComputedSignal.cs** - Optional computed values
6. **Update demo** - Update TodoList.razor to use signals

---

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Debounce location | In binding, not signal | Commands may want different debounce than UI updates |
| Cancel previous | Default on | Prevents stale results from slow requests |
| Disposal pattern | Required | Prevents memory leaks from event subscriptions |
| Implicit conversion | Included | `Signal<T>` can be used where `T` is expected |
| Multiple dependencies | Supported | Filter scenarios often have multiple inputs |
