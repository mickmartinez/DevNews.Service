using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevNews.Service.Domain.Entities;
using DevNews.Services.Application.Features.Weather.Exceptions;
using DevNews.Services.Application.Features.Weather.Interfaces;

namespace DevNews.Service.Infrastructure.Weather;

/// <summary>
/// <see cref="IWeatherForecastProvider"/> implementation backed by the free, keyless
/// Open-Meteo Geocoding and Forecast APIs. Resolves a city name to latitude/longitude
/// via the Geocoding API, then retrieves the current temperature and WMO weather code
/// via the Forecast API, mapping the latter to a human-readable summary.
/// </summary>
public class OpenMeteoWeatherForecastProvider : IWeatherForecastProvider
{
    private const string GenericUnavailableMessage =
        "The weather forecast provider is temporarily unavailable. Please try again later.";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyDictionary<int, string> WmoWeatherCodeSummaries = new Dictionary<int, string>
    {
        [0] = "Clear sky",
        [1] = "Mainly clear",
        [2] = "Partly cloudy",
        [3] = "Overcast",
        [45] = "Fog",
        [48] = "Depositing rime fog",
        [51] = "Light drizzle",
        [53] = "Moderate drizzle",
        [55] = "Dense drizzle",
        [56] = "Light freezing drizzle",
        [57] = "Dense freezing drizzle",
        [61] = "Slight rain",
        [63] = "Moderate rain",
        [65] = "Heavy rain",
        [66] = "Light freezing rain",
        [67] = "Heavy freezing rain",
        [71] = "Slight snow fall",
        [73] = "Moderate snow fall",
        [75] = "Heavy snow fall",
        [77] = "Snow grains",
        [80] = "Slight rain showers",
        [81] = "Moderate rain showers",
        [82] = "Violent rain showers",
        [85] = "Slight snow showers",
        [86] = "Heavy snow showers",
        [95] = "Thunderstorm",
        [96] = "Thunderstorm with slight hail",
        [99] = "Thunderstorm with heavy hail",
    };

    private readonly HttpClient _httpClient;

    public OpenMeteoWeatherForecastProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<WeatherForecast?> GetForecastAsync(string city, CancellationToken cancellationToken)
    {
        var geocodingResult = await SendAsync<GeocodingResponse>(
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json",
            cancellationToken);

        var match = geocodingResult.Results?.FirstOrDefault();
        if (match is null)
        {
            return null;
        }

        var forecastResult = await SendAsync<ForecastResponse>(
            $"https://api.open-meteo.com/v1/forecast?latitude={match.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={match.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current=temperature_2m,weather_code&timezone=auto",
            cancellationToken);

        if (forecastResult.Current is null)
        {
            throw new WeatherProviderUnavailableException(GenericUnavailableMessage);
        }

        var summary = WmoWeatherCodeSummaries.TryGetValue(forecastResult.Current.WeatherCode, out var mapped)
            ? mapped
            : "Unknown";

        return new WeatherForecast(
            match.Name,
            DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime),
            forecastResult.Current.Temperature2m,
            summary);
    }

    private async Task<T> SendAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new WeatherProviderUnavailableException(GenericUnavailableMessage, ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WeatherProviderUnavailableException(GenericUnavailableMessage, ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WeatherProviderUnavailableException(GenericUnavailableMessage, ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new WeatherProviderUnavailableException(GenericUnavailableMessage);
        }

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<T>(content, SerializerOptions);
            if (result is null)
            {
                throw new WeatherProviderUnavailableException(GenericUnavailableMessage);
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new WeatherProviderUnavailableException(GenericUnavailableMessage, ex);
        }
    }

    private sealed class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public List<GeocodingResult>? Results { get; set; }
    }

    private sealed class GeocodingResult
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    private sealed class ForecastResponse
    {
        [JsonPropertyName("current")]
        public CurrentWeather? Current { get; set; }
    }

    private sealed class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public double Temperature2m { get; set; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }
    }
}
