using BlazingSingularity.Commands;
using BlazingSingularity.Demo.Client.Weather.Dtos;
using BlazingSingularity.Demo.Client.Weather.HttpClients;
using BlazingSingularity.Signals;
using Microsoft.AspNetCore.Components;

namespace BlazingSingularity.Demo.Client.Weather.Pages;

public partial class Weather(
    IWeatherHttpClient weatherHttpClient,
    NavigationManager navigationManager
) : ComponentBase
{
    private WeatherForListDto[] forecasts = [];
    private WeatherForListDto[] filteredForecasts = [];

    [Signal]
    private string _summaryFilter = string.Empty;

    [Signal]
    private int? _minTemp = null;

    [Signal]
    private int? _maxTemp = null;

    protected override async Task OnInitializedAsync()
    {
        SummaryFilterSignal.OnChange(SearchCommand.RaiseCanExecuteChanged);
        MinTempSignal.OnChange(SearchCommand.RaiseCanExecuteChanged);
        MaxTempSignal.OnChange(SearchCommand.RaiseCanExecuteChanged);

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
            SummaryFilter,
            MinTemp,
            MaxTemp
        );
        filteredForecasts = results.ToArray();

        await InvokeAsync(StateHasChanged);
    }

    private bool CanSearch()
    {
        return forecasts.Length > 0
            && (
                !string.IsNullOrWhiteSpace(SummaryFilter)
                || MinTemp.HasValue
                || MaxTemp.HasValue
            );
    }

    private void ClearFilters()
    {
        SummaryFilter = string.Empty;
        MinTemp = null;
        MaxTemp = null;
        filteredForecasts = forecasts;
    }

    [RelayCommand]
    private async Task ViewForecastDetail(Guid forecastId)
    {
        navigationManager.NavigateTo($"weather/{forecastId}");
    }
}
