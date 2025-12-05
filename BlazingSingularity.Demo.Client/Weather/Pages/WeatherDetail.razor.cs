using BlazingSingularity.Commands;
using BlazingSingularity.Demo.Client.Weather.Dtos;
using BlazingSingularity.Demo.Client.Weather.HttpClients;
using Microsoft.AspNetCore.Components;

namespace BlazingSingularity.Demo.Client.Weather.Pages;

public partial class WeatherDetail : ComponentBase
{
    [Parameter]
    public Guid ForecastId { get; set; }

    [Inject]
    public required IWeatherHttpClient WeatherHttpClient { get; set; }

    private WeatherForListDto? weatherDetail { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await ViewWeatherDetailAsyncCommand.ExecuteAsync(ForecastId);
    }

    [RelayCommand]
    private async Task ViewWeatherDetailAsync(Guid id)
    {
        weatherDetail = await WeatherHttpClient.GetWeatherForecastByIdAsync(id);
    }
}
