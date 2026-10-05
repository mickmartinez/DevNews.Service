using DevNews.Service.Domain.Entities;
using DevNews.Service.Domain.Exceptions;
using Xunit;

namespace DevNews.Service.Tests.Domain.Entities;

public class WeatherForecastTests
{
    [Fact]
    public void GivenValidArguments_WhenConstructed_ThenPropertiesAreSet()
    {
        // Arrange
        var city = "Seattle";
        var date = new DateOnly(2026, 10, 5);
        var temperatureC = 18.5;
        var summary = "Cloudy";

        // Act
        var forecast = new WeatherForecast(city, date, temperatureC, summary);

        // Assert
        Assert.Equal(city, forecast.City);
        Assert.Equal(date, forecast.Date);
        Assert.Equal(temperatureC, forecast.TemperatureC);
        Assert.Equal(summary, forecast.Summary);
    }

    [Theory]
    [InlineData(0, 32)]
    [InlineData(100, 212)]
    [InlineData(-40, -40)]
    [InlineData(18.5, 65.3)]
    public void GivenTemperatureC_WhenTemperatureFComputed_ThenConversionIsCorrect(double temperatureC, double expectedFahrenheit)
    {
        // Arrange
        var forecast = new WeatherForecast("Seattle", new DateOnly(2026, 10, 5), temperatureC, "Cloudy");

        // Act
        var temperatureF = forecast.TemperatureF;

        // Assert
        Assert.Equal(expectedFahrenheit, temperatureF, precision: 1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidCity_WhenConstructed_ThenThrowsInvalidWeatherForecastException(string? city)
    {
        // Arrange
        var date = new DateOnly(2026, 10, 5);

        // Act
        var act = () => new WeatherForecast(city!, date, 20, "Sunny");

        // Assert
        Assert.Throws<InvalidWeatherForecastException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidSummary_WhenConstructed_ThenThrowsInvalidWeatherForecastException(string? summary)
    {
        // Arrange
        var date = new DateOnly(2026, 10, 5);

        // Act
        var act = () => new WeatherForecast("Seattle", date, 20, summary!);

        // Assert
        Assert.Throws<InvalidWeatherForecastException>(act);
    }

    [Fact]
    public void GivenTemperatureBelowAbsoluteZero_WhenConstructed_ThenThrowsInvalidWeatherForecastException()
    {
        // Arrange
        var date = new DateOnly(2026, 10, 5);
        var belowAbsoluteZero = -273.16;

        // Act
        var act = () => new WeatherForecast("Seattle", date, belowAbsoluteZero, "Sunny");

        // Assert
        Assert.Throws<InvalidWeatherForecastException>(act);
    }

    [Fact]
    public void GivenTemperatureAtAbsoluteZero_WhenConstructed_ThenDoesNotThrow()
    {
        // Arrange
        var date = new DateOnly(2026, 10, 5);
        var absoluteZero = -273.15;

        // Act
        var forecast = new WeatherForecast("Seattle", date, absoluteZero, "Sunny");

        // Assert
        Assert.Equal(absoluteZero, forecast.TemperatureC);
    }
}
