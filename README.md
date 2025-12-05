# BlazingSingularity - ICommand for Blazor

A WPF-style commanding pattern for Blazor using source generators.

## Features

- **`[RelayCommand]` attribute** - Decorate methods to automatically generate command properties
- **Async support** - Automatic `AsyncRelayCommand` generation for `Task` returning methods
- **`IsLoading` property** - Track async command execution state
- **`CanExecute` support** - Control command availability
- **Parameter support** - Commands support 0 or 1 parameter
- **CancellationToken support** - Cancel long-running async operations mid-execution
- **Fluent Builder API** - Create commands programmatically without attributes
- **Source generator** - Zero-overhead code generation at compile time

## Installation

### NuGet Package (Recommended)

```bash
dotnet add package BlazingSingularity --prerelease
```

Or via the Package Manager Console:

```powershell
Install-Package BlazingSingularity -Pre
```

The NuGet package includes both the library and the source generator automatically.

### Project Reference (Development)

If you're developing locally, add reference to both projects in your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\BlazingSingularity\BlazingSingularity.csproj" />
  <ProjectReference Include="..\BlazingSingularity.SourceGenerators\BlazingSingularity.SourceGenerators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Usage

### 1. Create a Partial Component Code-Behind

```csharp
using BlazingSingularity.Commands;
using Microsoft.AspNetCore.Components;

namespace YourNamespace;

public partial class MyComponent : ComponentBase
{
    private WeatherForecast[]? forecasts;

    // Async command with automatic CanExecute support
    [RelayCommand]
    private async Task LoadForecast()
    {
        await Task.Delay(1500);
        forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json");
        LoadForecastCommand.RaiseCanExecuteChanged();
        ClearCommand.RaiseCanExecuteChanged();
        StateHasChanged();
    }

    // CanExecute method - automatically discovered by source generator
    private bool CanLoadForecast()
    {
        // Can only load if we don't already have forecasts
        return forecasts == null;
    }

    [RelayCommand]
    private void Clear()
    {
        forecasts = null;
        LoadForecastCommand.RaiseCanExecuteChanged();
        ClearCommand.RaiseCanExecuteChanged();
    }

    private bool CanClear()
    {
        // Can only clear if we have forecasts
        return forecasts != null;
    }
}
```

### 2. Use Generated Commands in Razor

```razor
@inherits ComponentBase

<div>
    <button @onclick="() => LoadDataCommand.ExecuteAsync(\"test\")"
            disabled="@(!LoadDataCommand.CanExecute(\"test\"))">
        @if (LoadDataCommand.IsLoading)
        {
            <span>Loading...</span>
        }
        else
        {
            <span>Load Data</span>
        }
    </button>

    <button @onclick="() => SaveDataCommand.ExecuteAsync(null)"
            disabled="@(!SaveDataCommand.CanExecute(null))">
        @if (SaveDataCommand.IsLoading)
        {
            <span>Saving...</span>
        }
        else
        {
            <span>Save</span>
        }
    </button>
</div>
```

## Generated Code

For a method named `LoadForecast` with a `CanLoadForecast` method, the source generator creates:

```csharp
private AsyncRelayCommand? _loadForecastCommand;
public AsyncRelayCommand LoadForecastCommand => _loadForecastCommand ??= new(LoadForecast, CanLoadForecast);
```

Without a CanExecute method:
```csharp
private AsyncRelayCommand? _loadForecastCommand;
public AsyncRelayCommand LoadForecastCommand => _loadForecastCommand ??= new(LoadForecast);
```

## Command Properties

### AsyncRelayCommand&lt;T&gt; / AsyncRelayCommand

- `Task ExecuteAsync(object? parameter)` - Execute the async command
- `void Execute(object? parameter)` - Synchronous execution (fire-and-forget)
- `bool CanExecute(object? parameter)` - Check if command can execute
- `bool IsLoading` - Track if async operation is in progress
- `bool IsCancellable` - Check if command supports cancellation
- `void CancelCommand()` - Cancel the currently executing operation
- `event EventHandler? CanExecuteChanged` - Notification when CanExecute changes
- `event EventHandler? IsLoadingChanged` - Notification when IsLoading changes

### RelayCommand&lt;T&gt; / RelayCommand

- `void Execute(object? parameter)` - Execute the command
- `bool CanExecute(object? parameter)` - Check if command can execute
- `event EventHandler? CanExecuteChanged` - Notification when CanExecute changes

## CanExecute Support

The source generator automatically detects and wires up `CanExecute` methods:

1. **Naming Convention**: For a method `MyMethod`, create a method named `CanMyMethod` that returns `bool`
2. **Parameter Matching**: The `CanExecute` method must have the same parameters as the command method
3. **Automatic Wiring**: The generator automatically passes the `CanExecute` method to the command constructor
4. **Manual Notification**: Call `MyMethodCommand.RaiseCanExecuteChanged()` when conditions change

### Example:

```csharp
[RelayCommand]
private async Task LoadData(string filter)
{
    // ... load data
    LoadDataCommand.RaiseCanExecuteChanged(); // Notify that CanExecute state may have changed
}

private bool CanLoadData(string filter)
{
    return !string.IsNullOrEmpty(filter); // Only allow execution if filter is not empty
}
```

## CancellationToken Support

Commands can be cancelled mid-execution for long-running operations:

### Using Source Generator

Simply add a `CancellationToken` parameter to your method:

```csharp
[RelayCommand]
private async Task LoadData(CancellationToken cancellationToken)
{
    await Task.Delay(5000, cancellationToken);
    // Long-running operation that respects cancellation
}
```

The source generator automatically detects the `CancellationToken` and creates a cancellable command.

### In Razor

```razor
<button @onclick="() => LoadDataCommand.ExecuteAsync()"
        disabled="@LoadDataCommand.IsLoading">
    Load Data
</button>

@if (LoadDataCommand.IsLoading && LoadDataCommand.IsCancellable)
{
    <button @onclick="() => LoadDataCommand.CancelCommand()"
            class="btn-danger">
        Cancel
    </button>
}
```

### With Parameters

```csharp
[RelayCommand]
private async Task SearchData(string query, CancellationToken cancellationToken)
{
    await Task.Delay(3000, cancellationToken);
    // Search operation
}
```

The `CancellationToken` is always the last parameter and is automatically handled by the command infrastructure.

## Fluent Builder API

Create commands programmatically without using attributes:

### Sync Commands

```csharp
public ICommand SaveCommand => Commands.Create()
    .WithExecute(() =>
    {
        // Save logic
    })
    .WithCanExecute(() => _hasChanges)
    .Build();
```

### Async Commands

```csharp
public IAsyncCommand LoadCommand => Commands.CreateAsync()
    .WithExecute(async () =>
    {
        await LoadDataAsync();
    })
    .WithCanExecute(() => !string.IsNullOrEmpty(_filter))
    .Build();
```

### Async Commands with Cancellation

```csharp
public IAsyncCommand SearchCommand => Commands.CreateAsync<string>()
    .WithCancellation(async (query, ct) =>
    {
        await Task.Delay(3000, ct);
        // Search with cancellation support
    })
    .WithCanExecute(query => !string.IsNullOrWhiteSpace(query))
    .Build();
```

### Parameterized Commands

```csharp
// Sync with parameter
public ICommand DeleteCommand => Commands.Create<int>()
    .WithExecute(id => DeleteItem(id))
    .WithCanExecute(id => id > 0)
    .Build();

// Async with parameter
public IAsyncCommand UpdateCommand => Commands.CreateAsync<Item>()
    .WithExecute(async item => await UpdateItemAsync(item))
    .Build();
```

## Important Notes

1. **Class must be `partial`** - The class using `[RelayCommand]` must be declared as `partial`
2. **Supported signatures**:
   - `void MethodName()` → `RelayCommand`
   - `void MethodName(T param)` → `RelayCommand<T>`
   - `Task MethodName()` → `AsyncRelayCommand`
   - `Task MethodName(T param)` → `AsyncRelayCommand<T>`
   - `Task MethodName(CancellationToken ct)` → `AsyncRelayCommand` (Cancellable)
   - `Task MethodName(T param, CancellationToken ct)` → `AsyncRelayCommand<T>` (Cancellable)
3. **Naming convention** - Method `MyMethod` generates property `MyMethodCommand`
4. **IsLoading prevents execution** - When `IsLoading` is `true`, `CanExecute` automatically returns `false`
5. **CanExecute methods** - Must match parameter signature and return `bool` (excluding CancellationToken)
6. **CancellationToken** - Always the last parameter, automatically handled by command infrastructure

## Advanced Scenarios

### Multiple Filter Parameters

For complex filtering with many parameters, use **Bound Commands** that capture property values at execution time:

```csharp
public partial class MyComponent : ComponentBase
{
    // Filter properties bound to UI controls
    private string searchText = string.Empty;
    private int? minValue;
    private int? maxValue;
    private bool filterActive = true;

    // Bound command captures all properties when executed
    private IAsyncCommand? _searchCommand;
    public IAsyncCommand SearchCommand => _searchCommand ??= Commands.CreateAsync()
        .WithCancellation(async (ct) =>
        {
            // Properties captured at execution time
            var results = await SearchAsync(searchText, minValue, maxValue, filterActive, ct);
            await InvokeAsync(StateHasChanged);
        })
        .WithCanExecute(() => !string.IsNullOrWhiteSpace(searchText))
        .AsBound()  // Makes it capture properties at execution time
        .Build();

    private void OnFilterChanged()
    {
        SearchCommand.RaiseCanExecuteChanged();
    }
}
```

See [ADVANCED_FILTERING_EXAMPLE.md](ADVANCED_FILTERING_EXAMPLE.md) for complete examples.

## Project Structure

```
BlazingSingularity/
├── Commands/
│   ├── ICommand.cs                    - Command interface
│   ├── IAsyncCommand.cs               - Async command interface
│   ├── RelayCommand.cs                - Synchronous command implementation
│   ├── AsyncRelayCommand.cs           - Async command implementation
│   └── RelayCommandAttribute.cs       - Attribute for source generation
└── ExampleComponent.razor.cs          - Example usage

BlazingSingularity.SourceGenerators/
└── RelayCommandGenerator.cs           - Source generator implementation
```

## Example

See `ExampleComponent.razor` and `ExampleComponent.razor.cs` for a complete working example.
