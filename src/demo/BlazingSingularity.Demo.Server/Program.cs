using BlazingSingularity.Demo.Client.Weather.Dtos;
using BlazingSingularity.Demo.Components;
using BlazingSingularity.Demo.Features.Services;
using BlazingSingularity.Demo.Features.Todo;
using BlazingSingularity.Demo.Features.Todo.Endpoints;
using BlazingSingularity.Demo.Weather.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();

builder.Services.AddSingleton<WeatherService>();
builder.Services.AddSingleton<TodoRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazingSingularity.Demo.Client.AssemblyMarker).Assembly);


var api = app.MapGroup("/api").WithTags("API");

api.MapGet(
    "/weatherforecast",
    async (WeatherService weatherService, string? summary, int? minTemp, int? maxTemp) =>
    {
        // If no filters provided, return all forecasts
        if (string.IsNullOrWhiteSpace(summary) && !minTemp.HasValue && !maxTemp.HasValue)
        {
            return await weatherService.GetWeatherForecastAsync();
        }

        return await weatherService.SearchWeatherForecastAsync(summary, minTemp, maxTemp);
    }
);

api.MapGet(
    "/weatherforecast/{id:guid}",
    async (WeatherService weatherService, Guid id) =>
    {
        return await weatherService.GetWeatherForecastByIdAsync(id);
    }
);

api.MapTodoEndpoints();

app.Run();
