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

    private string summaryFilter = string.Empty;
    private int? minTemp = null;
    private int? maxTemp = null;

    protected override async Task OnInitializedAsync()
    {
        await LoadForecastsAsyncCommand.ExecuteAsync();
    }

    [RelayCommand]
    private async Task LoadForecastsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(2_000, cancellationToken);
        var forecast = await weatherHttpClient.GetWeatherForecastAsync();
        forecasts = forecast.ToArray();
        filteredForecasts = forecasts;

        SearchCommand.RaiseCanExecuteChanged();

        await InvokeAsync(StateHasChanged);
    }

    [RelayCommand]
    private async Task Search(CancellationToken cancellationToken)
    {
        var results = await weatherHttpClient.SearchWeatherForecastAsync(
            summaryFilter,
            minTemp,
            maxTemp
        );
        filteredForecasts = results.ToArray();

        await InvokeAsync(StateHasChanged);
    }

    private bool CanSearch()
    {
        return forecasts.Length > 0
            && (
                !string.IsNullOrWhiteSpace(summaryFilter)
                || minTemp.HasValue
                || maxTemp.HasValue
            );
    }

    private void OnFilterChanged()
    {
        SearchCommand.RaiseCanExecuteChanged();
    }

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
}
