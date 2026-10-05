using System.Text.Json;
using DevNews.Service.ExceptionHandling;
using DevNews.Services.Application.Features.Weather.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DevNews.Service.Tests.Api.ExceptionHandling;

public class WeatherApiExceptionHandlerTests
{
    private readonly WeatherApiExceptionHandler _handler = new();

    [Fact]
    public async Task GivenValidationException_WhenHandled_ThenReturns400WithGenericProblemDetails()
    {
        await AssertHandledExceptionAsync(
            new ValidationException(new[] { new ValidationFailure("City", "Sensitive validation detail") }),
            StatusCodes.Status400BadRequest,
            "The request failed validation.",
            "Sensitive validation detail");
    }

    [Fact]
    public async Task GivenCityNotFoundException_WhenHandled_ThenReturns404WithGenericProblemDetails()
    {
        await AssertHandledExceptionAsync(
            new CityNotFoundException("No forecast data available for city 'Atlantis'."),
            StatusCodes.Status404NotFound,
            "No forecast data is available for the requested city.",
            "Atlantis");
    }

    [Fact]
    public async Task GivenWeatherProviderUnavailableException_WhenHandled_ThenReturns503WithGenericProblemDetails()
    {
        await AssertHandledExceptionAsync(
            new WeatherProviderUnavailableException("Provider timeout from backend-42."),
            StatusCodes.Status503ServiceUnavailable,
            "The weather data source is temporarily unavailable. Please try again later.",
            "backend-42");
    }

    [Fact]
    public async Task GivenUnhandledException_WhenHandled_ThenReturns500WithGenericProblemDetails()
    {
        await AssertHandledExceptionAsync(
            new InvalidOperationException("Top secret internal failure."),
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred while processing the request.",
            "Top secret internal failure");
    }

    private async Task AssertHandledExceptionAsync(
        Exception exception,
        int expectedStatusCode,
        string expectedDetail,
        string leakedText)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(problemDetails);
        Assert.Equal(expectedStatusCode, problemDetails.Status);
        Assert.Equal(expectedDetail, problemDetails.Detail);
        Assert.DoesNotContain(exception.Message, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(leakedText, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", responseBody, StringComparison.OrdinalIgnoreCase);
    }
}
