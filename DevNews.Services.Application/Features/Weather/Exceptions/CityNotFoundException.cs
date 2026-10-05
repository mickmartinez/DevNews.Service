namespace DevNews.Services.Application.Features.Weather.Exceptions;

/// <summary>
/// Thrown by <c>GetWeatherForecastByCityQueryHandler</c> when the configured
/// <see cref="Interfaces.IWeatherForecastProvider"/> has no data for the requested
/// (syntactically valid) city. Mapped to HTTP 404 Not Found by the API layer.
/// </summary>
public sealed class CityNotFoundException : Exception
{
    public CityNotFoundException(string message)
        : base(message)
    {
    }
}
