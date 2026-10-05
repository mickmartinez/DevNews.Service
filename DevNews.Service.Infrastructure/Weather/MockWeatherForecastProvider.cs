using DevNews.Service.Domain.Entities;
using DevNews.Services.Application.Features.Weather.Interfaces;

namespace DevNews.Service.Infrastructure.Weather;

/// <summary>
/// In-memory/mock implementation of <see cref="IWeatherForecastProvider"/> used until a
/// real external weather data source is integrated. Seeded with a fixed set of supported
/// cities (e.g. Seattle, London, Tokyo, Sydney, Cairo), keyed by a case-insensitive,
/// normalized city name.
/// </summary>
public class MockWeatherForecastProvider : IWeatherForecastProvider
{
    private sealed record SeedForecast(string City, double TemperatureC, string Summary);

    private readonly IReadOnlyDictionary<string, SeedForecast> _seedsByCity;

    public MockWeatherForecastProvider()
    {
        var seedForecasts = new[]
        {
            new SeedForecast("Seattle", 18.5, "Cloudy"),
            new SeedForecast("London", 15.0, "Rainy"),
            new SeedForecast("Tokyo", 24.0, "Sunny"),
            new SeedForecast("Sydney", 22.5, "Clear"),
            new SeedForecast("Cairo", 32.0, "Hot"),
        };

        _seedsByCity = seedForecasts.ToDictionary(
            seed => seed.City,
            seed => seed,
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<WeatherForecast?> GetForecastAsync(string city, CancellationToken cancellationToken)
    {
        var normalizedCity = city?.Trim() ?? string.Empty;

        if (!_seedsByCity.TryGetValue(normalizedCity, out var seed))
        {
            return Task.FromResult<WeatherForecast?>(null);
        }

        // Always use the current date so the forecast never goes stale, even though the
        // provider instance itself is registered as a singleton.
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var forecast = new WeatherForecast(seed.City, today, seed.TemperatureC, seed.Summary);

        return Task.FromResult<WeatherForecast?>(forecast);
    }
}
