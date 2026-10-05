using FluentValidation;

namespace DevNews.Services.Application.Features.Weather.Queries;

/// <summary>
/// Validates <see cref="GetWeatherForecastByCityQuery"/> before it reaches the handler,
/// ensuring the city name is present, within the documented length limit, and contains
/// only characters valid for a city name.
/// </summary>
public class GetWeatherForecastByCityQueryValidator : AbstractValidator<GetWeatherForecastByCityQuery>
{
    private const int MaxCityLength = 100;
    private const string ValidCityNamePattern = @"^[\p{L} .'-]+$";

    public GetWeatherForecastByCityQueryValidator()
    {
        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City name is required.");

        RuleFor(x => x.City)
            .MaximumLength(MaxCityLength)
            .WithMessage($"City name must not exceed {MaxCityLength} characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.City)
            .Matches(ValidCityNamePattern)
            .WithMessage("City name contains invalid characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.City));
    }
}
