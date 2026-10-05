namespace DevNews.Service.Domain.Exceptions;

/// <summary>
/// Thrown when an attempt is made to construct a <see cref="Entities.WeatherForecast"/>
/// that violates one of its domain invariants (e.g. empty city, temperature below
/// absolute zero, or empty summary).
/// </summary>
public sealed class InvalidWeatherForecastException : Exception
{
    public InvalidWeatherForecastException(string message)
        : base(message)
    {
    }
}
