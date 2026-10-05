namespace DevNews.Services.Application.Features.Weather.Exceptions;

/// <summary>
/// Thrown or propagated when the underlying weather data source (via
/// <see cref="Interfaces.IWeatherForecastProvider"/>) is temporarily unable to
/// supply forecast data. Mapped to HTTP 503 Service Unavailable by the API layer.
/// Implementations of <c>IWeatherForecastProvider</c> are responsible for wrapping
/// low-level/provider-specific failures into this exception type so internal
/// implementation details never leak to the consumer.
/// </summary>
public sealed class WeatherProviderUnavailableException : Exception
{
    public WeatherProviderUnavailableException(string message)
        : base(message)
    {
    }

    public WeatherProviderUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
