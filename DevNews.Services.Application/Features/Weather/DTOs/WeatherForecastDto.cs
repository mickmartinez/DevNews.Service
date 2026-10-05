namespace DevNews.Services.Application.Features.Weather.DTOs;

/// <summary>
/// API-facing shape for a single weather forecast. This is the only type that
/// crosses the Application -> API boundary for the "get weather forecast by city"
/// feature; the Domain <c>WeatherForecast</c> entity is never returned directly.
/// </summary>
/// <param name="City">The city name the forecast pertains to.</param>
/// <param name="TemperatureC">Current temperature in degrees Celsius.</param>
/// <param name="TemperatureF">Current temperature in degrees Fahrenheit (computed from Celsius).</param>
/// <param name="Summary">Short weather condition text (e.g., "Sunny", "Rain").</param>
/// <param name="Date">The date the forecast pertains to (UTC date, no time component).</param>
public record WeatherForecastDto(
    string City,
    double TemperatureC,
    double TemperatureF,
    string Summary,
    DateOnly Date);
