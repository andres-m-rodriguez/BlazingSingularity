namespace BlazingSingularity.Demo.Client.Weather.Dtos;

public record WeatherForListDto(Guid Id, DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
