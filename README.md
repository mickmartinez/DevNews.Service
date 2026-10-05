# DevNews.Service

A .NET 10 Web API built with a strict **Layered Architecture** and the **CQRS** pattern (via [MediatR](https://github.com/jbogard/MediatR)).

## Architecture

The solution is split into four projects, each with a single responsibility and a one-way dependency flow:

```
DevNews.Service            (API)             -> Application, Infrastructure (DI composition root only)
DevNews.Services.Application (Application)   -> Domain
DevNews.Service.Infrastructure (Infrastructure) -> Application, Domain
DevNews.Service.Domain      (Domain)          -> (no dependencies)
DevNews.Service.Tests       (Tests)           -> all of the above
```

| Project | Responsibility |
|---|---|
| [DevNews.Service.Domain](/DevNews.Service.Domain) | Entities, value objects, and domain exceptions. Encapsulates business rules and invariants — no anemic models. |
| [DevNews.Services.Application](/DevNews.Services.Application) | CQRS Commands/Queries, MediatR handlers, DTOs, validators (FluentValidation), and the abstractions (interfaces) that Infrastructure implements. |
| [DevNews.Service.Infrastructure](/DevNews.Service.Infrastructure) | Concrete implementations of Application-owned abstractions (e.g. external API clients, EF Core persistence). |
| [DevNews.Service](/DevNews.Service) | ASP.NET Core Web API — controllers, OpenAPI configuration, global exception handling, and DI composition root (`Program.cs`). |
| [DevNews.Service.Tests](/DevNews.Service.Tests) | xUnit unit tests covering all layers. |

Key conventions enforced throughout the codebase:
- Commands mutate state, Queries read state — both dispatched through MediatR.
- Domain entities are never returned directly from API endpoints; everything is projected to DTOs in the Application layer.
- Read-only EF Core queries (where persistence is used) use `AsNoTracking()` and filter at the database level.
- Every class, record, or interface lives in its own file.
- API errors use standard HTTP status codes and `ProblemDetails`.

## Features

### Get weather forecast by city

`GET /api/weather/{city}` returns the current weather forecast for a given city.

The feature is implemented end-to-end following the architecture above:
- **Domain**: [`WeatherForecast`](/DevNews.Service.Domain/Entities/WeatherForecast.cs) entity encapsulates temperature-unit conversion (°C → °F) and validity invariants.
- **Application**: [`GetWeatherForecastByCityQuery`](/DevNews.Services.Application/Features/Weather/Queries/GetWeatherForecastByCityQuery.cs) + handler, a FluentValidation validator, and the `IWeatherForecastProvider` abstraction.
- **Infrastructure**: [`OpenMeteoWeatherForecastProvider`](/DevNews.Service.Infrastructure/Weather/OpenMeteoWeatherForecastProvider.cs) resolves the city via the free, keyless [Open-Meteo](https://open-meteo.com/) Geocoding API, then fetches current conditions from the Open-Meteo Forecast API. No API key or persistence is required.
- **API**: [`WeatherController`](/DevNews.Service/Controllers/WeatherController.cs) with full OpenAPI metadata and a global exception handler mapping failures to `400` (invalid city), `404` (unrecognized city), `503` (upstream unavailable), and `500`.

Example request/response:

```bash
curl http://localhost:5299/api/weather/Paris
```

```json
{
  "city": "Paris",
  "temperatureC": 21.6,
  "temperatureF": 70.88,
  "summary": "Overcast",
  "date": "2026-10-05"
}
```

See [docs/user-stories](/docs/user-stories) and [docs/specs](/docs/specs) for the full user story and technical specification behind this feature.

## Getting started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Build

```powershell
dotnet build
```

### Run

```powershell
dotnet run --project DevNews.Service
```

The API listens on the port configured in [launchSettings.json](/DevNews.Service/Properties/launchSettings.json). In the `Development` environment, the OpenAPI document is available at `/openapi/v1.json`.

### Test

```powershell
dotnet test
```

## Continuous Integration

[.github/workflows/ci.yml](/.github/workflows/ci.yml) builds the solution and runs the full test suite on every push and pull request targeting `main`.
