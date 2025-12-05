using BlazingSingularity.Demo.Client.Weather.Dtos;

namespace BlazingSingularity.Demo.Client.Weather.HttpClients;

public interface IWeatherHttpClient
{
    Task<List<WeatherForListDto>> GetWeatherForecastAsync();
    Task<WeatherForListDto?> GetWeatherForecastByIdAsync(Guid id);
    Task<List<WeatherForListDto>> SearchWeatherForecastAsync(
        string? summary,
        int? minTemp,
        int? maxTemp
    );
}
