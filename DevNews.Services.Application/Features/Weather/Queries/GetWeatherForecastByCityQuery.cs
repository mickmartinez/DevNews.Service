using DevNews.Services.Application.Features.Weather.DTOs;
using MediatR;

namespace DevNews.Services.Application.Features.Weather.Queries;

/// <summary>
/// Requests the current weather forecast for a given city name.
/// </summary>
/// <param name="City">
/// The raw city name as supplied by the API route parameter (validated and trimmed
/// by <see cref="GetWeatherForecastByCityQueryValidator"/> before the handler runs).
/// </param>
public record GetWeatherForecastByCityQuery(string City) : IRequest<WeatherForecastDto>;
