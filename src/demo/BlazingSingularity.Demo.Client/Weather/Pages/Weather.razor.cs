using BlazingSingularity.Commands;
using BlazingSingularity.Demo.Client.Weather.Dtos;
using BlazingSingularity.Demo.Client.Weather.HttpClients;
using Microsoft.AspNetCore.Components;

namespace BlazingSingularity.Demo.Client.Weather.Pages;

public partial class Weather(
    IWeatherHttpClient weatherHttpClient,
    NavigationManager navigationManager
) : ComponentBase
{
    private WeatherForListDto[] forecasts = [];
    private WeatherForListDto[] filteredForecasts = [];
    private WeatherForListDto? selectedForecastDetail = default!;

    // Multiple filter properties
    private string summaryFilter = string.Empty;
    private int? minTemp = null;
    private int? maxTemp = null;

    // Bound command for multiple filters - demonstrates BoundAsyncRelayCommand
    private IAsyncCommand? _searchCommand;
    public IAsyncCommand SearchCommand =>
        _searchCommand ??= RelayCommands
            .CreateAsync()
            .WithCancellation(
                async (ct) =>
                {
                    // All filter properties captured at execution time
                    var results = await weatherHttpClient.SearchWeatherForecastAsync(
                        summaryFilter,
                        minTemp,
                        maxTemp
                    );
                    filteredForecasts = results.ToArray();

                    await InvokeAsync(StateHasChanged);
                }
            )
            .WithCanExecute(() =>
            {
                // Can search if we have forecasts and at least one filter is set
                return forecasts.Length > 0
                    && (
                        !string.IsNullOrWhiteSpace(summaryFilter)
                        || minTemp.HasValue
                        || maxTemp.HasValue
                    );
            })
            .AsBound() // Captures property values at execution time
            .Build();

    protected override async Task OnInitializedAsync()
    {
        await LoadForecastsAsyncCommand.ExecuteAsync();
    }

    // Example with CancellationToken support using source generator
    [RelayCommand]
    private async Task LoadForecastsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(2_000, cancellationToken);
        var forecast = await weatherHttpClient.GetWeatherForecastAsync();
        forecasts = forecast.ToArray();
        filteredForecasts = forecasts;

        // Notify search command that data is loaded
        SearchCommand.RaiseCanExecuteChanged();

        await InvokeAsync(StateHasChanged);
    }

    // Called when any filter changes
    private void OnFilterChanged()
    {
        SearchCommand.RaiseCanExecuteChanged();
    }

    // Reset all filters
    private void ClearFilters()
    {
        summaryFilter = string.Empty;
        minTemp = null;
        maxTemp = null;
        filteredForecasts = forecasts;
        SearchCommand.RaiseCanExecuteChanged();
    }

    [RelayCommand]
    private async Task ViewForecastDetail(Guid forecastId)
    {
        navigationManager.NavigateTo($"weather/{forecastId}");
    }

    private void CloseForecastDetail()
    {
        selectedForecastDetail = null;
    }
}
