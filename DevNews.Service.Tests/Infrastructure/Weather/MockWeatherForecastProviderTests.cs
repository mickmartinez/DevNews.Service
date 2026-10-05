using DevNews.Service.Infrastructure.Weather;
using Xunit;

namespace DevNews.Service.Tests.Infrastructure.Weather;

/// <summary>
/// Unit tests for <see cref="MockWeatherForecastProvider"/>, the in-memory/mock
/// implementation of <c>IWeatherForecastProvider</c> seeded with a fixed set of
/// supported cities (Seattle, London, Tokyo, Sydney, Cairo).
/// </summary>
public class MockWeatherForecastProviderTests
{
    private readonly MockWeatherForecastProvider _provider = new();

    [Theory]
    [InlineData("Seattle")]
    [InlineData("London")]
    [InlineData("Tokyo")]
    [InlineData("Sydney")]
    [InlineData("Cairo")]
    public async Task GivenKnownSeededCity_WhenGetForecastAsync_ThenReturnsForecastForThatCity(string city)
    {
        // Arrange
        // (provider is seeded in its constructor)

        // Act
        var forecast = await _provider.GetForecastAsync(city, CancellationToken.None);

        // Assert
        Assert.NotNull(forecast);
        Assert.Equal(city, forecast!.City, ignoreCase: true);
    }

    [Theory]
    [InlineData("seattle")]
    [InlineData("SEATTLE")]
    [InlineData("sEaTTle")]
    [InlineData("lOnDoN")]
    public async Task GivenSeededCityInDifferentCasing_WhenGetForecastAsync_ThenLookupIsCaseInsensitive(string city)
    {
        // Act
        var forecast = await _provider.GetForecastAsync(city, CancellationToken.None);

        // Assert
        Assert.NotNull(forecast);
        Assert.Equal(city, forecast!.City, ignoreCase: true);
    }

    [Fact]
    public async Task GivenUnknownCity_WhenGetForecastAsync_ThenReturnsNull()
    {
        // Arrange
        const string unknownCity = "Atlantis";

        // Act
        var forecast = await _provider.GetForecastAsync(unknownCity, CancellationToken.None);

        // Assert
        Assert.Null(forecast);
    }

    [Fact]
    public async Task GivenUnknownCity_WhenGetForecastAsync_ThenDoesNotThrow()
    {
        // Arrange
        const string unknownCity = "Nonexistentville";

        // Act
        var exception = await Record.ExceptionAsync(
            () => _provider.GetForecastAsync(unknownCity, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task GivenKnownSeededCityWithSurroundingWhitespaceAlreadyTrimmed_WhenGetForecastAsync_ThenLookupDoesNotThrow()
    {
        // Arrange
        const string city = "Seattle";

        // Act
        var exception = await Record.ExceptionAsync(
            () => _provider.GetForecastAsync(city, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
}
