using DevNews.Services.Application.Features.Weather.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevNews.Service.ExceptionHandling;

/// <summary>
/// Global exception handler that maps Application-layer exceptions (and any unhandled
/// exception) to a <see cref="ProblemDetails"/> response with the correct HTTP status
/// code. Only fixed, generic messages are ever surfaced in <c>Detail</c> — raw exception
/// messages and stack traces are never exposed to the client.
/// </summary>
public class WeatherApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "The request failed validation."),
            CityNotFoundException => (
                StatusCodes.Status404NotFound,
                "City not found.",
                "No forecast data is available for the requested city."),
            WeatherProviderUnavailableException => (
                StatusCodes.Status503ServiceUnavailable,
                "Weather data source unavailable.",
                "The weather data source is temporarily unavailable. Please try again later."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "An unexpected error occurred while processing the request."),
        };

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
