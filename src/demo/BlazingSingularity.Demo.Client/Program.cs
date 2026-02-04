using BlazingSingularity.Demo.Client.Todo.HttpClients;
using BlazingSingularity.Demo.Client.Weather.HttpClients;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.ConfigureHttpClientDefaults(httpClientBuilderOptions =>
{
    httpClientBuilderOptions.ConfigureHttpClient(httpClientConfigs =>
    {
        httpClientConfigs.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    });
});
builder.Services.AddHttpClient<IWeatherHttpClient, WeatherHttpClient>();
builder.Services.AddHttpClient<TodoHttpClient>();

await builder.Build().RunAsync();
