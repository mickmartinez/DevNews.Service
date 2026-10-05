using DevNews.Services.Application.Features.Weather.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DevNews.Service.Infrastructure.Weather;

/// <summary>
/// DI composition-root helper that registers the Infrastructure implementations of the
/// weather-forecast feature's Application-owned abstractions.
/// </summary>
public static class WeatherInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IWeatherForecastProvider, MockWeatherForecastProvider>();

        return services;
    }
}
