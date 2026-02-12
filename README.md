# BlazingSingularity

[![Build](https://github.com/andres-m-rodriguez/BlazingSingularity/actions/workflows/build.yml/badge.svg)](https://github.com/andres-m-rodriguez/BlazingSingularity/actions/workflows/build.yml)

WPF-style `[RelayCommand]` for Blazor, powered by source generators.

## Installation

```bash
dotnet add package BlazingSingularity --prerelease
```

## Usage

### Code-behind

```csharp
using BlazingSingularity.Commands;
using Microsoft.AspNetCore.Components;

namespace MyApp.Pages;

public partial class Counter : ComponentBase
{
    private int count = 0;

    [RelayCommand]
    private void Increment()
    {
        count++;
    }

    [RelayCommand]
    private async Task LoadData(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
    }
}
```

```razor
@page "/counter"

<p>Count: @count</p>

<button @onclick="() => IncrementCommand.Execute()">Increment</button>

<button @onclick="() => LoadDataCommand.ExecuteAsync()"
        disabled="@LoadDataCommand.IsLoading">
    Load
</button>
```

### Inline `@code` block

```razor
@page "/todos"
@using BlazingSingularity.Commands

<button @onclick="() => AddTodoCommand.Execute()">Add</button>

@foreach (var todo in todos)
{
    <p>@todo</p>
}

@code {
    private List<string> todos = [];

    [RelayCommand]
    private void AddTodo()
    {
        todos.Add($"Todo #{todos.Count + 1}");
    }
}
```

### Error Handling with `TryExecuteAsync`

Use `TryExecuteAsync` for functional-style error handling with the `Result` monad:

```csharp
var result = await LoadDataCommand.TryExecuteAsync();

result.Match(
    onSuccess: () => Console.WriteLine("Data loaded successfully"),
    onFailure: ex => Console.WriteLine($"Failed: {ex.Message}")
);

// Or use pattern matching
if (result.IsFailure)
{
    ErrorMessage = result.ErrorMessage;
    return;
}
```

The `Result` type provides:
- `IsSuccess` / `IsFailure` - Check the outcome
- `Exception` - Access the exception on failure
- `ErrorMessage` - Shorthand for `Exception?.Message`
- `Match` - Functional pattern matching with callbacks

### Reactive State with `[Signal]`

Use `[Signal]` to create reactive state that can notify subscribers when values change:

```csharp
using BlazingSingularity.Signals;

public partial class SearchPage : ComponentBase
{
    [Signal]
    private string _searchText = string.Empty;

    [Signal]
    private int? _minPrice = null;

    protected override void OnInitialized()
    {
        // Subscribe to changes
        SearchTextSignal.OnChange(newValue =>
        {
            Console.WriteLine($"Search changed to: {newValue}");
        });

        // Trigger command CanExecute re-evaluation on change
        MinPriceSignal.OnChange(SearchCommand.RaiseCanExecuteChanged);
    }

    [RelayCommand]
    private void Search() { /* ... */ }

    private bool CanSearch() => !string.IsNullOrWhiteSpace(SearchText);
}
```

The source generator creates:
- A public property (`SearchText`) that notifies on change
- A `Signal<T>` accessor (`SearchTextSignal`) for subscribing to changes

Use the generated property in your Razor markup:

```razor
<input @bind="SearchText" @bind:event="oninput" />
```
