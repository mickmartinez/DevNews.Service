using DevNews.Service.Controllers;
using DevNews.Services.Application.Features.Weather.DTOs;
using DevNews.Services.Application.Features.Weather.Exceptions;
using DevNews.Services.Application.Features.Weather.Queries;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Xunit;

namespace DevNews.Service.Tests.Api.Controllers;

/// <summary>
/// Unit tests for <see cref="WeatherController"/>. The <see cref="ISender"/> dependency
/// is mocked so these tests exercise only the controller's own HTTP-shaping behavior
/// (status codes / payloads), never a real MediatR pipeline or real handler.
/// </summary>
public class WeatherControllerTests
{
    private readonly Mock<ISender> _mockSender;
    private readonly WeatherController _controller;

    public WeatherControllerTests()
    {
        _mockSender = new Mock<ISender>();
        _controller = new WeatherController(_mockSender.Object);
    }

    [Fact]
    public async Task GivenValidCity_WhenGetByCity_ThenReturns200OkWithDto()
    {
        // Arrange
        var dto = new WeatherForecastDto("Seattle", 20, 68, "Sunny", DateOnly.FromDateTime(DateTime.UtcNow));
        _mockSender
            .Setup(s => s.Send(It.IsAny<GetWeatherForecastByCityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetByCity("Seattle", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task GivenCityNotFoundException_WhenGetByCity_ThenPropagatesExceptionForGlobalHandler()
    {
        // Arrange
        _mockSender
            .Setup(s => s.Send(It.IsAny<GetWeatherForecastByCityQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CityNotFoundException("No forecast data available for city 'Atlantis'."));

        // Act & Assert
        // The controller no longer catches Application exceptions itself; they are left to
        // propagate so the global IExceptionHandler can map them to a ProblemDetails response.
        await Assert.ThrowsAsync<CityNotFoundException>(
            () => _controller.GetByCity("Atlantis", CancellationToken.None));
    }

    [Fact]
    public async Task GivenWeatherProviderUnavailableException_WhenGetByCity_ThenPropagatesExceptionForGlobalHandler()
    {
        // Arrange
        _mockSender
            .Setup(s => s.Send(It.IsAny<GetWeatherForecastByCityQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WeatherProviderUnavailableException("The weather data source is temporarily unavailable."));

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderUnavailableException>(
            () => _controller.GetByCity("Seattle", CancellationToken.None));
    }

    [Fact]
    public async Task GivenValidationFailure_WhenGetByCity_ThenPropagatesExceptionForGlobalHandler()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("City", "City name contains invalid characters.") };
        _mockSender
            .Setup(s => s.Send(It.IsAny<GetWeatherForecastByCityQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _controller.GetByCity("Inv@lid!!", CancellationToken.None));
    }
}
