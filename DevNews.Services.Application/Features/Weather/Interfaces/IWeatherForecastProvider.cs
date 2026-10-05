using DevNews.Service.Domain.Entities;

namespace DevNews.Services.Application.Features.Weather.Interfaces;

/// <summary>
/// Abstraction over the underlying weather data source, owned by the Application
/// layer and implemented by Infrastructure (e.g. a mock/in-memory provider or a
/// future pluggable external weather API client).
/// </summary>
public interface IWeatherForecastProvider
{
    /// <summary>
    /// Retrieves the current weather forecast for the given (already validated and
    /// trimmed) city name.
    /// </summary>
    /// <param name="city">The normalized city name to retrieve a forecast for.</param>
    /// <param name="cancellationToken">Token used to abort the operation.</param>
    /// <returns>
    /// The <see cref="WeatherForecast"/> for the city, or <c>null</c> if the city is
    /// not recognized by the data source.
    /// </returns>
    /// <exception cref="Exceptions.WeatherProviderUnavailableException">
    /// Thrown when the data source cannot currently be reached/queried.
    /// </exception>
    Task<WeatherForecast?> GetForecastAsync(string city, CancellationToken cancellationToken);
}
