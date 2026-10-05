using DevNews.Service.Domain.Entities;
using DevNews.Services.Application.Features.Weather.DTOs;
using DevNews.Services.Application.Features.Weather.Exceptions;
using DevNews.Services.Application.Features.Weather.Interfaces;
using MediatR;

namespace DevNews.Services.Application.Features.Weather.Queries;

/// <summary>
/// Handles <see cref="GetWeatherForecastByCityQuery"/> by delegating retrieval to the
/// configured <see cref="IWeatherForecastProvider"/> and projecting the result to a
/// <see cref="WeatherForecastDto"/>.
/// </summary>
public class GetWeatherForecastByCityQueryHandler(IWeatherForecastProvider weatherForecastProvider)
    : IRequestHandler<GetWeatherForecastByCityQuery, WeatherForecastDto>
{
    public async Task<WeatherForecastDto> Handle(GetWeatherForecastByCityQuery request, CancellationToken cancellationToken)
    {
        var normalizedCity = request.City.Trim();

        var forecast = await weatherForecastProvider.GetForecastAsync(normalizedCity, cancellationToken);

        if (forecast is null)
            throw new CityNotFoundException($"No forecast data available for city '{normalizedCity}'.");

        return ToDto(forecast);
    }

    private static WeatherForecastDto ToDto(WeatherForecast forecast) =>
        new(forecast.City, forecast.TemperatureC, forecast.TemperatureF, forecast.Summary, forecast.Date);
}
