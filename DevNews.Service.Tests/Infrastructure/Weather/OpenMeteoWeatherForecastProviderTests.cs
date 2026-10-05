using System.Net;
using DevNews.Service.Infrastructure.Weather;
using DevNews.Services.Application.Features.Weather.Exceptions;
using Xunit;

namespace DevNews.Service.Tests.Infrastructure.Weather;

/// <summary>
/// Red-phase unit tests for <see cref="OpenMeteoWeatherForecastProvider"/>, the
/// HttpClient-based implementation of <c>IWeatherForecastProvider</c> backed by the
/// free, keyless Open-Meteo Geocoding and Forecast APIs.
///
/// A fake <see cref="HttpMessageHandler"/> test double is injected into the
/// <see cref="HttpClient"/> passed to the provider's constructor so that no real
/// network calls are made. The provider itself is not implemented yet
/// (<see cref="NotImplementedException"/> is thrown), so all tests below are expected
/// to fail for that reason during this Red phase.
/// </summary>
public class OpenMeteoWeatherForecastProviderTests
{
    private const string GeocodingUrlFragment = "geocoding-api.open-meteo.com";
    private const string ForecastUrlFragment = "api.open-meteo.com";

    private static string GeocodingResponseJson(string canonicalName, double lat, double lon) =>
        $$"""
        {
          "results": [
            {
              "name": "{{canonicalName}}",
              "latitude": {{lat}},
              "longitude": {{lon}}
            }
          ]
        }
        """;

    private const string EmptyGeocodingResponseJson = """
        {
          "results": []
        }
        """;

    private static string ForecastResponseJson(double temperatureC, int weatherCode) =>
        $$"""
        {
          "current": {
            "temperature_2m": {{temperatureC}},
            "weather_code": {{weatherCode}}
          }
        }
        """;

    private static HttpClient CreateHttpClient(FakeHttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://example-unused.invalid/") };

    [Fact]
    public async Task GivenKnownCity_WhenGetForecastAsync_ThenReturnsForecastWithCanonicalNameTemperatureAndMappedSummary()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains(GeocodingUrlFragment, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GeocodingResponseJson("Seattle", 47.6062, -122.3321))
                };
            }

            if (request.RequestUri!.Host.Contains(ForecastUrlFragment, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ForecastResponseJson(18.5, 0)) // 0 = Clear sky
                };
            }

            throw new InvalidOperationException("Unexpected request URI: " + request.RequestUri);
        });

        var provider = new OpenMeteoWeatherForecastProvider(CreateHttpClient(handler));

        // Act
        var forecast = await provider.GetForecastAsync("seattle", CancellationToken.None);

        // Assert
        Assert.NotNull(forecast);
        Assert.Equal("Seattle", forecast!.City);
        Assert.Equal(18.5, forecast.TemperatureC);
        Assert.False(string.IsNullOrWhiteSpace(forecast.Summary));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GivenUnknownCity_WhenGetForecastAsync_ThenReturnsNullAndDoesNotCallForecastApi()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains(GeocodingUrlFragment, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(EmptyGeocodingResponseJson)
                };
            }

            throw new InvalidOperationException("Forecast API should not be called for an unknown city.");
        });

        var provider = new OpenMeteoWeatherForecastProvider(CreateHttpClient(handler));

        // Act
        var forecast = await provider.GetForecastAsync("Atlantis", CancellationToken.None);

        // Assert
        Assert.Null(forecast);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GivenGeocodingCallReturnsHttpErrorStatus_WhenGetForecastAsync_ThenThrowsWeatherProviderUnavailableException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var provider = new OpenMeteoWeatherForecastProvider(CreateHttpClient(handler));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<WeatherProviderUnavailableException>(
            () => provider.GetForecastAsync("Seattle", CancellationToken.None));

        Assert.DoesNotContain("api.open-meteo.com", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("geocoding-api.open-meteo.com", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GivenGeocodingCallThrowsHttpRequestException_WhenGetForecastAsync_ThenThrowsWeatherProviderUnavailableException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ =>
            throw new HttpRequestException("Simulated network failure."));

        var provider = new OpenMeteoWeatherForecastProvider(CreateHttpClient(handler));

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderUnavailableException>(
            () => provider.GetForecastAsync("Seattle", CancellationToken.None));
    }

    [Fact]
    public async Task GivenForecastCallReturnsHttpErrorStatus_WhenGetForecastAsync_ThenThrowsWeatherProviderUnavailableException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains(GeocodingUrlFragment, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GeocodingResponseJson("Seattle", 47.6062, -122.3321))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        var provider = new OpenMeteoWeatherForecastProvider(CreateHttpClient(handler));

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderUnavailableException>(
            () => provider.GetForecastAsync("Seattle", CancellationToken.None));
    }

    [Fact]
    public async Task GivenForecastCallThrowsHttpRequestException_WhenGetForecastAsync_ThenThrowsWeatherProviderUnavailableException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains(GeocodingUrlFragment, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GeocodingResponseJson("Seattle", 47.6062, -122.3321))
                };
            }

            throw new HttpRequestException("Simulated network failure.");
        });

        var provider = new OpenMeteoWeatherForecastProvider(CreateHttpClient(handler));

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderUnavailableException>(
            () => provider.GetForecastAsync("Seattle", CancellationToken.None));
    }

    [Fact]
    public async Task GivenRequestTimesOut_WhenGetForecastAsync_ThenThrowsWeatherProviderUnavailableException()
    {
        // Arrange: simulate an internal timeout (TaskCanceledException NOT caused by the
        // caller's own CancellationToken, which is CancellationToken.None here).
        var handler = new FakeHttpMessageHandler(_ =>
            throw new TaskCanceledException("Simulated timeout.", new TimeoutException()));

        var provider = new OpenMeteoWeatherForecastProvider(CreateHttpClient(handler));

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderUnavailableException>(
            () => provider.GetForecastAsync("Seattle", CancellationToken.None));
    }

    [Fact]
    public async Task GivenMalformedGeocodingJson_WhenGetForecastAsync_ThenThrowsWeatherProviderUnavailableException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains(GeocodingUrlFragment, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{ not valid json ")
                };
            }

            throw new InvalidOperationException("Forecast API should not be called when geocoding JSON is malformed.");
        });

        var provider = new OpenMeteoWeatherForecastProvider(CreateHttpClient(handler));

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderUnavailableException>(
            () => provider.GetForecastAsync("Seattle", CancellationToken.None));
    }

    [Fact]
    public async Task GivenMalformedForecastJson_WhenGetForecastAsync_ThenThrowsWeatherProviderUnavailableException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains(GeocodingUrlFragment, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GeocodingResponseJson("Seattle", 47.6062, -122.3321))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ not valid json ")
            };
        });

        var provider = new OpenMeteoWeatherForecastProvider(CreateHttpClient(handler));

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderUnavailableException>(
            () => provider.GetForecastAsync("Seattle", CancellationToken.None));
    }

    [Fact]
    public async Task GivenUnrecognizedWmoWeatherCode_WhenGetForecastAsync_ThenSucceedsWithGenericUnknownSummary()
    {
        // Arrange
        const int unrecognizedWmoCode = 9999;

        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains(GeocodingUrlFragment, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GeocodingResponseJson("Seattle", 47.6062, -122.3321))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ForecastResponseJson(10.0, unrecognizedWmoCode))
            };
        });

        var provider = new OpenMeteoWeatherForecastProvider(CreateHttpClient(handler));

        // Act
        var forecast = await provider.GetForecastAsync("Seattle", CancellationToken.None);

        // Assert
        Assert.NotNull(forecast);
        Assert.Equal("Unknown", forecast!.Summary);
    }

    /// <summary>
    /// Minimal, allocation-free test double for <see cref="HttpMessageHandler"/> that
    /// delegates each request to a supplied function, avoiding any real network I/O.
    /// Records each request made so tests can assert on call counts (e.g. that the
    /// forecast API is never called when geocoding yields no results).
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public List<HttpRequestMessage> Requests { get; } = new();

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var response = _responder(request);
            return Task.FromResult(response);
        }
    }
}
