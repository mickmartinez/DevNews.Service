using System.ComponentModel.DataAnnotations;
using DevNews.Services.Application.Features.Weather.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DevNews.Service.Controllers;

/// <summary>
/// Exposes the "get weather forecast by city" read model over HTTP.
/// </summary>
[ApiController]
[Route("api/weather")]
public class WeatherController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Gets the current weather forecast for the given city.
    /// </summary>
    /// <param name="city">
    /// The name of the city to retrieve the current weather forecast for. Leading/trailing
    /// whitespace is trimmed. Max length 100 characters. Must contain only letters, spaces,
    /// hyphens, apostrophes, and periods (e.g., "Seattle", "St. Louis", "Winston-Salem").
    /// </param>
    /// <param name="cancellationToken">Token used to abort the request.</param>
    /// <returns>The current weather forecast for the requested city.</returns>
    /// <response code="200">Weather forecast retrieved successfully.</response>
    /// <response code="400">
    /// Validation error — city missing, empty, too long, or contains invalid characters.
    /// </response>
    /// <response code="404">No forecast data available for the specified city.</response>
    /// <response code="503">The underlying weather data source is temporarily unavailable.</response>
    [HttpGet("{city}")]
    [Tags("Weather")]
    [EndpointSummary("Get the current weather forecast for a city")]
    [EndpointDescription(
        "Retrieves the current weather forecast (temperature, condition, and date) for the given city name.")]
    [ProducesResponseType(typeof(DevNews.Services.Application.Features.Weather.DTOs.WeatherForecastDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByCity(
        [StringLength(100)]
        [RegularExpression(@"^[\p{L} .'-]+$")]
        string city,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWeatherForecastByCityQuery(city), cancellationToken);
        return Ok(result);
    }
}
