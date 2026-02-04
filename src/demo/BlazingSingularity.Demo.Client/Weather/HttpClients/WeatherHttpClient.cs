using System.Net.Http.Json;
using BlazingSingularity.Demo.Client.Weather.Dtos;

namespace BlazingSingularity.Demo.Client.Weather.HttpClients;

public class WeatherHttpClient(HttpClient httpClient) : IWeatherHttpClient
{
    public async Task<List<WeatherForListDto>> GetWeatherForecastAsync()
    {
        var weatherForecast = await httpClient.GetFromJsonAsync<List<WeatherForListDto>>(
            "api/weatherforecast"
        );
        return weatherForecast ?? [];
    }

    public Task<WeatherForListDto?> GetWeatherForecastByIdAsync(Guid id)
    {
        return httpClient.GetFromJsonAsync<WeatherForListDto?>($"api/weatherforecast/{id}");
    }

    public async Task<List<WeatherForListDto>> SearchWeatherForecastAsync(
        string? summary,
        int? minTemp,
        int? maxTemp
    )
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(summary))
            queryParams.Add($"summary={Uri.EscapeDataString(summary)}");

        if (minTemp.HasValue)
            queryParams.Add($"minTemp={minTemp.Value}");

        if (maxTemp.HasValue)
            queryParams.Add($"maxTemp={maxTemp.Value}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var results = await httpClient.GetFromJsonAsync<List<WeatherForListDto>>(
            $"api/weatherforecast{queryString}"
        );
        return results ?? new List<WeatherForListDto>();
    }
}
