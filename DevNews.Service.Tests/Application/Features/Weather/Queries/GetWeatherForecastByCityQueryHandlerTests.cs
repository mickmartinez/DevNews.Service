using DevNews.Service.Domain.Entities;
using DevNews.Services.Application.Features.Weather.Exceptions;
using DevNews.Services.Application.Features.Weather.Interfaces;
using DevNews.Services.Application.Features.Weather.Queries;
using Moq;
using Xunit;

namespace DevNews.Service.Tests.Application.Features.Weather.Queries;

public class GetWeatherForecastByCityQueryHandlerTests
{
    private readonly Mock<IWeatherForecastProvider> _mockProvider;
    private readonly GetWeatherForecastByCityQueryHandler _handler;

    public GetWeatherForecastByCityQueryHandlerTests()
    {
        _mockProvider = new Mock<IWeatherForecastProvider>();
        _handler = new GetWeatherForecastByCityQueryHandler(_mockProvider.Object);
    }

    [Fact]
    public async Task GivenCityKnownToProvider_WhenHandled_ThenReturnsMappedDto()
    {
        // Arrange
        var date = new DateOnly(2026, 10, 5);
        var forecast = new WeatherForecast("Seattle", date, 18.5, "Cloudy");
        var query = new GetWeatherForecastByCityQuery("Seattle");

        _mockProvider
            .Setup(p => p.GetForecastAsync("Seattle", It.IsAny<CancellationToken>()))
            .ReturnsAsync(forecast);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(forecast.City, result.City);
        Assert.Equal(forecast.TemperatureC, result.TemperatureC);
        Assert.Equal(forecast.TemperatureF, result.TemperatureF);
        Assert.Equal(forecast.Summary, result.Summary);
        Assert.Equal(forecast.Date, result.Date);
    }

    [Fact]
    public async Task GivenCityKnownToProvider_WhenHandled_ThenProviderIsCalledExactlyOnce()
    {
        // Arrange
        var date = new DateOnly(2026, 10, 5);
        var forecast = new WeatherForecast("Seattle", date, 18.5, "Cloudy");
        var query = new GetWeatherForecastByCityQuery("Seattle");

        _mockProvider
            .Setup(p => p.GetForecastAsync("Seattle", It.IsAny<CancellationToken>()))
            .ReturnsAsync(forecast);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockProvider.Verify(
            p => p.GetForecastAsync("Seattle", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenCityWithSurroundingWhitespace_WhenHandled_ThenProviderIsCalledWithTrimmedCity()
    {
        // Arrange
        var date = new DateOnly(2026, 10, 5);
        var forecast = new WeatherForecast("Seattle", date, 18.5, "Cloudy");
        var query = new GetWeatherForecastByCityQuery("  Seattle  ");

        _mockProvider
            .Setup(p => p.GetForecastAsync("Seattle", It.IsAny<CancellationToken>()))
            .ReturnsAsync(forecast);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockProvider.Verify(
            p => p.GetForecastAsync("Seattle", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenCityUnknownToProvider_WhenHandled_ThenThrowsCityNotFoundException()
    {
        // Arrange
        var query = new GetWeatherForecastByCityQuery("Atlantis");

        _mockProvider
            .Setup(p => p.GetForecastAsync("Atlantis", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherForecast?)null);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CityNotFoundException>(act);
    }

    [Fact]
    public async Task GivenProviderThrowsWeatherProviderUnavailableException_WhenHandled_ThenExceptionPropagates()
    {
        // Arrange
        var query = new GetWeatherForecastByCityQuery("Seattle");
        var providerException = new WeatherProviderUnavailableException("The weather data source is temporarily unavailable.");

        _mockProvider
            .Setup(p => p.GetForecastAsync("Seattle", It.IsAny<CancellationToken>()))
            .ThrowsAsync(providerException);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        var thrown = await Assert.ThrowsAsync<WeatherProviderUnavailableException>(act);
        Assert.Same(providerException, thrown);
    }
}
