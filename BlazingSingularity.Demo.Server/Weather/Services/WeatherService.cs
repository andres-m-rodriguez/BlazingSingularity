using BlazingSingularity.Demo.Client.Weather.Dtos;

namespace BlazingSingularity.Demo.Weather.Services;

public class WeatherService
{
    private WeatherForListDto[] forecasts { get; }

    public WeatherService()
    {
        var startDate = DateOnly.FromDateTime(DateTime.Now);
        List<string> summaries = new List<string>
        {
            "Freezing",
            "Bracing",
            "Chilly",
            "Cool",
            "Mild",
            "Warm",
            "Balmy",
            "Hot",
            "Sweltering",
            "Scorching",
        };
        var forecasts = Enumerable
            .Range(1, 5)
            .Select(index => new WeatherForListDto(
                Guid.CreateVersion7(),
                startDate.AddDays(index),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Count)]
            ));
        this.forecasts = forecasts.ToArray();
    }

    public async Task<WeatherForListDto[]> GetWeatherForecastAsync()
    {
        await Task.Delay(2_000);
        return forecasts;
    }

    public async Task<WeatherForListDto?> GetWeatherForecastByIdAsync(Guid id)
    {
        await Task.Delay(1_000);
        return forecasts.FirstOrDefault(f => f.Id == id);
    }

    public async Task<WeatherForListDto[]> SearchWeatherForecastAsync(
        string? summary,
        int? minTemp,
        int? maxTemp
    )
    {
        // Simulate network delay to demonstrate cancellation
        await Task.Delay(1_500);

        var results = forecasts.AsEnumerable();

        // Apply summary filter
        if (!string.IsNullOrWhiteSpace(summary))
        {
            results = results.Where(f =>
                f.Summary?.Contains(summary, StringComparison.OrdinalIgnoreCase) == true
            );
        }

        // Apply min temperature filter
        if (minTemp.HasValue)
        {
            results = results.Where(f => f.TemperatureC >= minTemp.Value);
        }

        // Apply max temperature filter
        if (maxTemp.HasValue)
        {
            results = results.Where(f => f.TemperatureC <= maxTemp.Value);
        }

        return results.ToArray();
    }
}
