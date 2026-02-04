# BlazingSingularity

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
