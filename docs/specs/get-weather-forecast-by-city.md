# Get Weather Forecast by City

**Status**: Draft
**Created**: 2026-10-05
**Author**: spec-writer agent
**Related Stories**: [docs/user-stories/get-weather-forecast-by-city-user-story.md](../user-stories/get-weather-forecast-by-city-user-story.md)

## Executive Summary

This feature exposes a single read-only endpoint that returns the current weather forecast for a given city name. It is implemented as a MediatR Query (`GetWeatherForecastByCityQuery`) handled in the Application layer, which delegates data retrieval to an `IWeatherForecastProvider` abstraction. The Infrastructure layer supplies an `HttpClient`-based implementation of that provider backed by the free, keyless [Open-Meteo](https://open-meteo.com/) Geocoding and Forecast APIs — no EF Core/persistence are involved. The Domain layer owns a `WeatherForecast` entity that encapsulates the temperature/unit conversion business rule (Celsius → Fahrenheit) and basic invariants, ensuring the Application layer never leaks domain types to the API by projecting to a `WeatherForecastDto`.

## Technical Analysis

### Affected Layers

- **Domain (`DevNews.Service.Domain`)**:
  - New entity `WeatherForecast` (City, Date, TemperatureC, Summary; computed `TemperatureF`).
  - New domain exception `InvalidWeatherForecastException` (thrown when invariants are violated when constructing the entity, e.g. empty city or temperature below absolute zero).
  - No Domain Events required (no persistence, no state transition to broadcast).

- **Application (`DevNews.Services.Application`)**:
  - New query `GetWeatherForecastByCityQuery` (city: string) returning `WeatherForecastDto`.
  - New handler `GetWeatherForecastByCityQueryHandler`.
  - New DTO `WeatherForecastDto` (City, TemperatureC, TemperatureF, Summary, Date).
  - New FluentValidation validator `GetWeatherForecastByCityQueryValidator` (required, max length, allowed characters, trims whitespace).
  - New provider abstraction `IWeatherForecastProvider` (interface owned by Application, implemented by Infrastructure).
  - New Application-level exceptions: `CityNotFoundException` (maps to 404), `WeatherProviderUnavailableException` (maps to 503).

- **Infrastructure (`DevNews.Service.Infrastructure`)**:
  - New class `OpenMeteoWeatherForecastProvider : IWeatherForecastProvider` — an `HttpClient`-based implementation that calls the Open-Meteo Geocoding API (`GET https://geocoding-api.open-meteo.com/v1/search?name={city}&count=1`) to resolve a city name to latitude/longitude, then the Open-Meteo Forecast API (`GET https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code`) to retrieve the current temperature and WMO weather code, which is mapped to a human-readable summary.
  - Both Open-Meteo APIs are free and keyless, so no API-key/secret configuration is required.
  - Typed `HttpClient` registered via `IHttpClientFactory` (`services.AddHttpClient<OpenMeteoWeatherForecastProvider>(...)`) with a short request timeout (e.g. 5s) to keep the endpoint responsive. No single `BaseAddress` is configured on the typed client because the Geocoding (`geocoding-api.open-meteo.com`) and Forecast (`api.open-meteo.com`) APIs live on two different hosts; the provider instead issues absolute request URIs for both calls.
  - DI registration extension (e.g., `AddWeatherInfrastructure(this IServiceCollection services)`) to register `IWeatherForecastProvider` → `OpenMeteoWeatherForecastProvider`.
  - No EF Core `DbContext` changes, no entity configurations, no migrations required for this feature.

- **API (`DevNews.Service`)**:
  - New controller `WeatherController` (or Minimal API endpoint group) exposing `GET /api/weather/{city}`, routing to MediatR via `ISender`/`IMediator`.
  - Global exception-handling middleware (or a mapping filter) translates Application exceptions to `ProblemDetails` with the correct status codes (400 / 404 / 503 / 500).
  - No authorization policy required (anonymous access), consistent with the user story's security notes. If a global `[Authorize]` policy exists project-wide, this endpoint must be explicitly annotated `[AllowAnonymous]` to preserve the documented contract.

### Affected Layers — Dependency Direction

```mermaid
flowchart LR
    API --> Application
    Application --> Domain
    Infrastructure --> Application
    Infrastructure --> Domain
    API --> Infrastructure_DI[DI Composition Root only]
```

The API layer composes DI (registering `OpenMeteoWeatherForecastProvider` against `IWeatherForecastProvider`) but has no compile-time dependency on Infrastructure types beyond the composition root (`Program.cs`).

## API Contract

```yaml
paths:
  /api/weather/{city}:
    get:
      summary: Get the current weather forecast for a city
      operationId: getWeatherForecastByCity
      tags:
        - Weather
      parameters:
        - name: city
          in: path
          required: true
          description: >
            The name of the city to retrieve the current weather forecast for.
            Leading/trailing whitespace is trimmed. Max length 100 characters.
            Must contain only letters, spaces, hyphens, apostrophes, and periods
            (e.g., "Seattle", "St. Louis", "Winston-Salem").
          schema:
            type: string
            maxLength: 100
            example: Seattle
      responses:
        '200':
          description: Weather forecast retrieved successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/WeatherForecastDto'
        '400':
          description: Validation error — city missing, empty, too long, or contains invalid characters
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
        '404':
          description: No forecast data available for the specified city
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
        '500':
          description: Unexpected internal server error
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
        '503':
          description: Underlying weather data source is temporarily unavailable
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'

components:
  schemas:
    WeatherForecastDto:
      type: object
      required:
        - city
        - temperatureC
        - temperatureF
        - summary
        - date
      properties:
        city:
          type: string
          example: Seattle
        temperatureC:
          type: number
          format: double
          description: Current temperature in degrees Celsius
          example: 18.5
        temperatureF:
          type: number
          format: double
          description: Current temperature in degrees Fahrenheit (computed from Celsius)
          example: 65.3
        summary:
          type: string
          description: Short weather condition text
          example: Cloudy
        date:
          type: string
          format: date
          description: The date the forecast pertains to (UTC date, no time component)
          example: "2026-10-05"
    ProblemDetails:
      type: object
      properties:
        type:
          type: string
        title:
          type: string
        status:
          type: integer
        detail:
          type: string
        instance:
          type: string
```

### HTTP Status Code Summary

| Status | Condition | Source |
|---|---|---|
| `200 OK` | Valid, recognized city; forecast returned | Handler success path |
| `400 Bad Request` | City missing/empty/whitespace, exceeds max length, or contains invalid characters | FluentValidation pipeline behavior (MediatR) |
| `404 Not Found` | City is syntactically valid but not recognized by the provider | `CityNotFoundException` → exception-handling middleware |
| `503 Service Unavailable` | Provider throws a transient/availability failure | `WeatherProviderUnavailableException` → exception-handling middleware |
| `500 Internal Server Error` | Any unhandled exception | Global fallback exception handler |

Error responses never include stack traces, exception messages from internals, or provider-specific diagnostics in `detail`; only a generic, safe message is surfaced.

## Domain Architecture (`DevNews.Service.Domain`)

### Entity Relationship

```mermaid
classDiagram
    class WeatherForecast {
        +string City
        +DateOnly Date
        +double TemperatureC
        +string Summary
        +double TemperatureF
        +WeatherForecast(city, date, temperatureC, summary)
    }
    class InvalidWeatherForecastException {
        +InvalidWeatherForecastException(message)
    }
    WeatherForecast ..> InvalidWeatherForecastException : throws on invariant violation
```

### Entity Design — `WeatherForecast`

| Property | Type | Constraints / Attributes |
|---|---|---|
| `City` | `string` | Required, non-empty, trimmed, max 100 chars (validated by caller before construction; entity also guards against empty/whitespace) |
| `Date` | `DateOnly` | Required; represents the date the forecast pertains to |
| `TemperatureC` | `double` | Required; must be ≥ `-273.15` (absolute zero) |
| `Summary` | `string` | Required, non-empty short text (e.g., "Sunny", "Rain") |
| `TemperatureF` | `double` (computed, read-only) | Derived as `(TemperatureC * 9 / 5) + 32`; not settable directly |

### Domain Behavior

- The entity is constructed only via a constructor (or static factory `WeatherForecast.Create(...)`) that validates:
  - `City` is not null/empty/whitespace → otherwise throws `InvalidWeatherForecastException("City is required.")`.
  - `TemperatureC` is not below absolute zero (`-273.15`) → otherwise throws `InvalidWeatherForecastException("Temperature cannot be below absolute zero.")`.
  - `Summary` is not null/empty/whitespace → otherwise throws `InvalidWeatherForecastException("Summary is required.")`.
- `TemperatureF` is a computed property with no public setter — it is always derived from `TemperatureC`, guaranteeing the two values can never be inconsistent.
- The entity is immutable after construction (no public setters); this is a value-like entity since it is never persisted or looked up by identity — it does not need an `Id`/`Guid` primary key for this minimal scope.

### Domain Exceptions

- `InvalidWeatherForecastException : Exception` — thrown by the `WeatherForecast` constructor/factory when invariants listed above are violated. This is a safety net; the primary validation (city format/length) happens in the Application layer's FluentValidation validator before the query even reaches the handler, so this exception is expected to be rare (e.g., only triggered by malformed provider data).

## Application Layer & CQRS (`DevNews.Services.Application`)

### Queries (Read-Only)

- **Name**: `GetWeatherForecastByCityQuery`
- **Properties**:
  - `City` (`string`) — the raw city name as supplied by the API route parameter.
- **Return Type**: `WeatherForecastDto`
- **Validation** (`GetWeatherForecastByCityQueryValidator : AbstractValidator<GetWeatherForecastByCityQuery>`):
  - `RuleFor(x => x.City).NotEmpty().WithMessage("City name is required.")`
  - `RuleFor(x => x.City).MaximumLength(100).WithMessage("City name must not exceed 100 characters.")`
  - `RuleFor(x => x.City).Matches(@"^[\p{L} .'-]+$").WithMessage("City name contains invalid characters.")` (applied after trimming; allows letters/unicode letters, spaces, periods, apostrophes, hyphens — rejects digits/control characters/symbols)
  - Validation runs via a shared MediatR `ValidationBehavior<TRequest, TResponse>` pipeline behavior that short-circuits and surfaces a `ValidationException`, mapped to `400 Bad Request` with a `ProblemDetails` body by the API layer.
- **Handler Logic** (`GetWeatherForecastByCityQueryHandler : IRequestHandler<GetWeatherForecastByCityQuery, WeatherForecastDto>`):
  1. Normalize the city name: `Trim()` the input (already guaranteed non-empty/valid by the validator).
  2. Call `IWeatherForecastProvider.GetForecastAsync(normalizedCity, cancellationToken)` exactly once (no redundant/repeated calls within the request).
  3. If the provider returns `null` (city not recognized), throw `CityNotFoundException($"No forecast data available for city '{city}'.")`.
  4. If the provider throws a transient/availability exception, let it propagate as `WeatherProviderUnavailableException` (the provider implementation is responsible for wrapping/translating low-level failures into this Application-level exception type so Infrastructure details never leak).
  5. Construct/obtain the Domain `WeatherForecast` entity (either returned directly by the provider or constructed in the handler from primitive data returned by the provider) and project it to `WeatherForecastDto` via a `static` mapping method or AutoMapper `ProjectTo`-style explicit mapping (no AutoMapper dependency is required given the minimal shape — manual mapping is acceptable and preferred to keep this Infrastructure-free).
  - The entire handler method is `async` and threads the incoming `CancellationToken` through to `IWeatherForecastProvider.GetForecastAsync`.

### DTOs

- **`WeatherForecastDto`** (record or class, Application layer, API-facing shape):

  | Property | Type |
  |---|---|
  | `City` | `string` |
  | `TemperatureC` | `double` |
  | `TemperatureF` | `double` |
  | `Summary` | `string` |
  | `Date` | `DateOnly` |

  The Domain entity is never returned directly from the handler — the DTO is the only type that crosses the Application → API boundary for this query.

### Application-Level Exceptions

- **`CityNotFoundException`** — thrown by the handler when the provider has no data for the requested city. Maps to `404 Not Found`.
- **`WeatherProviderUnavailableException`** — thrown/propagated when the provider cannot currently supply data. Maps to `503 Service Unavailable`.

### Provider Abstraction (owned by Application, implemented by Infrastructure)

```csharp
public interface IWeatherForecastProvider
{
    Task<WeatherForecast?> GetForecastAsync(string city, CancellationToken cancellationToken);
}
```

- Returns `null` when the city is not recognized (geocoding API returns no results; handler translates this to `CityNotFoundException`).
- Throws `WeatherProviderUnavailableException` when the geocoding or forecast HTTP calls fail, time out, or return an unparsable/unexpected response — the implementation wraps the low-level `HttpRequestException`/`TaskCanceledException`/deserialization failure so no transport or API-specific detail leaks to the consumer.
- Returns a Domain `WeatherForecast` entity (constructed internally by the Infrastructure implementation from the Open-Meteo response), keeping entity construction co-located with the layer that owns the raw provider data.

## Infrastructure Layer (`DevNews.Service.Infrastructure`)

- **`OpenMeteoWeatherForecastProvider : IWeatherForecastProvider`**:
  - Step 1 — **Geocoding**: calls `GET {geocoding-base}/v1/search?name={city}&count=1&language=en&format=json` to resolve the city name to a `latitude`/`longitude` pair and a canonical `name`. An empty/missing `results` array means the city is not recognized → returns `null`.
  - Step 2 — **Forecast**: calls `GET {forecast-base}/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code&timezone=auto` to retrieve the current `temperature_2m` (°C) and `weather_code` (WMO code).
  - Maps the numeric WMO `weather_code` to a short human-readable `Summary` (e.g. 0 → "Clear sky", 61 → "Rain") via an internal lookup table; unknown codes fall back to a generic "Unknown" summary rather than throwing.
  - Constructs the `WeatherForecast` entity using the canonical city name from the geocoding response, today's date (`DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime)`), the returned temperature, and the mapped summary.
  - Any `HttpRequestException`, non-success status code, timeout (`TaskCanceledException`/`OperationCanceledException` not caused by the caller's own `CancellationToken`), or JSON deserialization failure is caught and re-thrown as `WeatherProviderUnavailableException` with a generic message (no raw exception text/URLs in the message).
  - No `DbContext`, no `IEntityTypeConfiguration<T>`, no migrations — this feature has zero persistence footprint.
  - No API key/secret is required — both Open-Meteo endpoints are free and keyless.
- **DI Registration**: an `IServiceCollection` extension method (e.g., `AddWeatherInfrastructure`) in Infrastructure registers a typed `HttpClient` for `OpenMeteoWeatherForecastProvider` (via `AddHttpClient<>`) with a request timeout, and binds `IWeatherForecastProvider` → `OpenMeteoWeatherForecastProvider`. No `BaseAddress` is set on the typed client since the two Open-Meteo APIs are hosted on different domains; the provider builds absolute URIs for each call instead.

## Performance & Green Code Considerations

- **Single provider call per request**: the handler calls `IWeatherForecastProvider.GetForecastAsync` exactly once — no redundant/repeated fetches within the request lifecycle (the provider itself makes two outbound HTTP calls — geocoding then forecast — but this is an internal implementation detail of a single logical lookup).
- **No collection/pagination concerns**: the endpoint returns a single object, not a list, so no pagination, `Skip`/`Take`, or max-page-size rules apply.
- **Async all the way down**: the controller action, MediatR handler, and provider method are all `async` and accept/forward a `CancellationToken` sourced from `HttpContext.RequestAborted`, threaded into both outbound `HttpClient` calls.
- **No tracking / no EF Core overhead**: this feature has no database interaction at all, eliminating any `AsNoTracking()` concerns.
- **Bounded external calls**: the typed `HttpClient` has a short timeout (e.g. 5s) so a slow/unresponsive upstream API cannot hang the request indefinitely; a timeout is translated into `WeatherProviderUnavailableException` (503), never left to bubble up as an unhandled 500.
- **Safe error messages**: `ProblemDetails.Detail` for 404/503/500 responses contains only generic, pre-defined messages — never raw exception messages, stack traces, upstream URLs, or provider implementation details, minimizing both security risk and unnecessary response payload size.

## Testing Requirements (`DevNews.Service.Tests`)

### Domain Tests

- [ ] `GivenEmptyCity_WhenWeatherForecastCreated_ThenThrowsInvalidWeatherForecastException`
- [ ] `GivenTemperatureBelowAbsoluteZero_WhenWeatherForecastCreated_ThenThrowsInvalidWeatherForecastException`
- [ ] `GivenEmptySummary_WhenWeatherForecastCreated_ThenThrowsInvalidWeatherForecastException`
- [ ] `GivenValidTemperatureC_WhenTemperatureFComputed_ThenReturnsCorrectFahrenheitConversion`
- [ ] `GivenValidInputs_WhenWeatherForecastCreated_ThenPropertiesAreSetCorrectly`

### Application Tests (Validator)

- [ ] `GivenEmptyOrWhitespaceCity_WhenValidated_ThenFluentValidationFails`
- [ ] `GivenCityExceedingMaxLength_WhenValidated_ThenFluentValidationFails`
- [ ] `GivenCityWithInvalidCharacters_WhenValidated_ThenFluentValidationFails`
- [ ] `GivenValidCityName_WhenValidated_ThenFluentValidationSucceeds`
- [ ] `GivenCityWithLeadingOrTrailingWhitespace_WhenValidated_ThenValidationSucceedsAfterTrim`

### Application Tests (Handler)

- [ ] `GivenRecognizedCity_WhenQueryHandled_ThenReturnsWeatherForecastDtoWithExpectedShape`
- [ ] `GivenUnrecognizedCity_WhenQueryHandled_ThenThrowsCityNotFoundException`
- [ ] `GivenProviderThrowsUnavailableException_WhenQueryHandled_ThenExceptionPropagatesAsWeatherProviderUnavailableException`
- [ ] `GivenValidQuery_WhenHandled_ThenProviderIsCalledExactlyOnce`
- [ ] `GivenCancellationRequested_WhenQueryHandled_ThenCancellationTokenIsHonored`

### API Tests

- [ ] `GivenExistingCity_WhenGetWeatherEndpointCalled_ThenReturns200WithExpectedJsonShape`
- [ ] `GivenMissingOrEmptyCity_WhenGetWeatherEndpointCalled_ThenReturns400WithProblemDetails`
- [ ] `GivenCityExceedingMaxLengthOrInvalidCharacters_WhenGetWeatherEndpointCalled_ThenReturns400WithProblemDetails`
- [ ] `GivenUnrecognizedCity_WhenGetWeatherEndpointCalled_ThenReturns404WithProblemDetails`
- [ ] `GivenProviderUnavailable_WhenGetWeatherEndpointCalled_ThenReturns503WithProblemDetailsAndNoLeakedInternals`
- [ ] `GivenUnhandledException_WhenGetWeatherEndpointCalled_ThenReturns500WithGenericProblemDetails`

## Quality Standards Checklist

- [x] Domain layer has no data-access or HTTP concerns — `WeatherForecast` is a pure, immutable entity with constructor-enforced invariants.
- [x] Application layer has no HTTP/routing concerns — the query/handler/validator are fully decoupled from `HttpContext` and routing.
- [x] CQRS strictly separated — this feature introduces only a Query (`GetWeatherForecastByCityQuery`); no Command is required since no state is mutated.
- [x] DTO (`WeatherForecastDto`) is the only type exposed across the API boundary; the `WeatherForecast` Domain entity is never serialized directly.
- [x] Validation rules, entity invariants, and response shapes are fully specified for implementation without further clarification.
