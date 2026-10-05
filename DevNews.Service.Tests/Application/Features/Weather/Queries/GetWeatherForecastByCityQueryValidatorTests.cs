using DevNews.Services.Application.Features.Weather.Queries;
using Xunit;

namespace DevNews.Service.Tests.Application.Features.Weather.Queries;

public class GetWeatherForecastByCityQueryValidatorTests
{
    private readonly GetWeatherForecastByCityQueryValidator _validator = new();

    [Fact]
    public void GivenValidCity_WhenValidated_ThenNoValidationErrors()
    {
        // Arrange
        var query = new GetWeatherForecastByCityQuery("Seattle");

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenMissingOrEmptyCity_WhenValidated_ThenFailsWithRequiredError(string? city)
    {
        // Arrange
        var query = new GetWeatherForecastByCityQuery(city!);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWeatherForecastByCityQuery.City));
    }

    [Fact]
    public void GivenCityExceedingMaxLength_WhenValidated_ThenFailsWithMaxLengthError()
    {
        // Arrange
        var tooLongCity = new string('a', 101);
        var query = new GetWeatherForecastByCityQuery(tooLongCity);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public void GivenCityAtMaxLength_WhenValidated_ThenNoValidationErrors()
    {
        // Arrange
        var maxLengthCity = new string('a', 100);
        var query = new GetWeatherForecastByCityQuery(maxLengthCity);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Seattle123")]
    [InlineData("12345")]
    [InlineData("Se@ttle")]
    [InlineData("Seattle!")]
    [InlineData("Seattle_WA")]
    public void GivenCityWithInvalidCharacters_WhenValidated_ThenFailsWithInvalidCharactersError(string city)
    {
        // Arrange
        var query = new GetWeatherForecastByCityQuery(city);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("invalid characters"));
    }

    [Theory]
    [InlineData("St. Louis")]
    [InlineData("Winston-Salem")]
    [InlineData("O'Fallon")]
    [InlineData("Seattle")]
    public void GivenCityWithAllowedSpecialCharacters_WhenValidated_ThenNoValidationErrors(string city)
    {
        // Arrange
        var query = new GetWeatherForecastByCityQuery(city);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }
}
