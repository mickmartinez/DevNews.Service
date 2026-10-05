using DevNews.Service.Domain.Exceptions;

namespace DevNews.Service.Domain.Entities;

/// <summary>
/// Represents the current weather forecast for a single city at a single point in time.
/// This is a value-like entity: it has no identity/primary key, is immutable after
/// construction, and is never persisted.
/// </summary>
public class WeatherForecast
{
    private const double AbsoluteZeroCelsius = -273.15;

    public string City { get; private set; }
    public DateOnly Date { get; private set; }
    public double TemperatureC { get; private set; }
    public string Summary { get; private set; }

    /// <summary>
    /// Temperature expressed in degrees Fahrenheit, always derived from
    /// <see cref="TemperatureC"/> so the two values can never be inconsistent.
    /// </summary>
    public double TemperatureF => (TemperatureC * 9 / 5) + 32;

    public WeatherForecast(string city, DateOnly date, double temperatureC, string summary)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new InvalidWeatherForecastException("City is required.");

        if (temperatureC < AbsoluteZeroCelsius)
            throw new InvalidWeatherForecastException("Temperature cannot be below absolute zero.");

        if (string.IsNullOrWhiteSpace(summary))
            throw new InvalidWeatherForecastException("Summary is required.");

        City = city;
        Date = date;
        TemperatureC = temperatureC;
        Summary = summary;
    }
}
