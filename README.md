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

### Async Data Fetching with `Fetch<T>`

Use `Fetch<T>` for reactive async data loading with built-in loading/error/success states:

```csharp
using BlazingSingularity.Fetch;

public partial class TodosPage : ComponentBase, IDisposable
{
    [Inject] private HttpClient Http { get; set; } = null!;

    private Fetch<List<Todo>> TodosFetch = null!;

    protected override async Task OnInitializedAsync()
    {
        TodosFetch = new Fetch<List<Todo>>(
            ct => Http.GetFromJsonAsync<List<Todo>>("/api/todos", ct)!,
            () => InvokeAsync(StateHasChanged)
        );
        await TodosFetch.FetchAsync();
    }

    public void Dispose() => TodosFetch.Dispose();
}
```

```razor
@if (TodosFetch.IsLoading) { <Spinner /> }
else if (TodosFetch.IsError) { <p>Error: @TodosFetch.Error?.Message</p> }
else if (TodosFetch.HasData)
{
    @foreach (var todo in TodosFetch.Data!)
    {
        <p>@todo.Title</p>
    }
}
```

### Stale-While-Revalidate with `BackgroundFetch<T>`

Use `BackgroundFetch<T>` to show cached data instantly while refreshing in the background:

```csharp
private BackgroundFetch<OrgDto> OrgFetch = null!;

protected override async Task OnInitializedAsync()
{
    OrgFetch = new BackgroundFetch<OrgDto>(
        ct => Http.GetFromJsonAsync<OrgDto>($"/api/orgs/{OrgId}", ct)!,
        () => InvokeAsync(StateHasChanged)
    );
    OrgFetch.SetInitialData(CachedOrg);  // Display immediately
    await OrgFetch.FetchAsync();          // Refresh in background
}
```

```razor
<div class="@(OrgFetch.IsStale ? "opacity-50" : "")">
    @if (OrgFetch.IsRefreshing) { <SmallSpinner /> }
    <h1>@OrgFetch.Data?.Name</h1>
</div>
```
