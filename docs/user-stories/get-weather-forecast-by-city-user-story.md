# User Story: Get Current Weather Forecast by City

**As an** API consumer,
**I want to** request the current weather forecast for a given city name,
**so that** I can display basic, up-to-date weather information (temperature, condition, and date) to my end users without needing to manage my own weather data source.

## Description

API consumers (e.g., the DevNews front-end or other integrating clients) need a simple, read-only way to retrieve the current weather forecast for a city. This is a minimal-scope feature: no historical data, no multi-day forecasts, and no persistence of weather data in the DevNews database. The weather data itself may originate from an in-memory/mock provider or a pluggable external weather data source abstraction, but the consumer-facing contract only needs to expose basic forecast fields: city, temperature, summary/condition, and date.

This endpoint is read-only and stateless from the consumer's perspective — each request simply returns the current forecast for the requested city at the time of the call.

## Scenario

### Scenario 1: Successful retrieval of weather forecast for a valid city

**Given** the API consumer sends a request specifying an existing, supported city name (e.g., "Seattle")
**When** the consumer issues a `GET` request to the weather forecast endpoint with that city name
**Then** the API responds with HTTP `200 OK`
**And** the response body contains the city name, current temperature, a weather summary/condition (e.g., "Cloudy", "Sunny"), and the forecast date

### Scenario 2: City name is missing or empty

**Given** the API consumer sends a request without providing a city name (or an empty/whitespace-only value)
**When** the consumer issues a `GET` request to the weather forecast endpoint
**Then** the API responds with HTTP `400 Bad Request`
**And** the response body is a `ProblemDetails` object describing that the city name is required

### Scenario 3: City name is not recognized/supported

**Given** the API consumer sends a request specifying a city name that the weather data source does not recognize
**When** the consumer issues a `GET` request to the weather forecast endpoint with that city name
**Then** the API responds with HTTP `404 Not Found`
**And** the response body is a `ProblemDetails` object indicating that no forecast data is available for the specified city

### Scenario 4: City name exceeds allowed length or contains invalid characters

**Given** the API consumer sends a request with a city name that exceeds the maximum allowed length or contains invalid characters (e.g., digits, symbols)
**When** the consumer issues a `GET` request to the weather forecast endpoint with that value
**Then** the API responds with HTTP `400 Bad Request`
**And** the response body is a `ProblemDetails` object describing the validation failure

### Scenario 5: Underlying weather data source is unavailable

**Given** the configured weather data source (external or internal) is temporarily unavailable or fails to respond
**When** the consumer issues a `GET` request to the weather forecast endpoint
**Then** the API responds with an appropriate server error status (e.g., HTTP `503 Service Unavailable`)
**And** the response body is a `ProblemDetails` object indicating the forecast could not be retrieved at this time, without leaking internal implementation details

## Acceptance Criteria

### Core Functionality
- A `GET` endpoint accepts a city name (e.g., as a route or query parameter) and returns the current weather forecast for that city.
- On success, the API returns HTTP `200 OK` with a JSON body containing at minimum:
  - City name (as provided or normalized)
  - Current temperature (with a clear, documented unit — e.g., Celsius)
  - Weather summary/condition (short descriptive text, e.g., "Sunny", "Rain")
  - Forecast date (the date the forecast data pertains to)
- Each request returns fresh/current data for the requested city — no stale cached response is guaranteed beyond what the underlying weather data source itself provides.
- Repeated requests for the same city return consistent field structure.

### Input Validation
- City name is a required parameter; a missing, null, empty, or whitespace-only value results in HTTP `400 Bad Request` with a `ProblemDetails` response.
- City name has a documented maximum length; exceeding it results in HTTP `400 Bad Request`.
- City name must not contain characters invalid for a city name (e.g., digits-only values, control characters); violations result in HTTP `400 Bad Request`.
- Leading/trailing whitespace in the city name is trimmed before processing and does not cause a validation failure.

### Domain/Business Rules
- The forecast is scoped to a single, current point-in-time result per request — this endpoint does not return multi-day or historical forecasts.
- If the city name does not match any city known to the underlying weather data source, the API returns HTTP `404 Not Found` rather than a `200` with empty/null data.
- The weather data source used to fulfill the request is an implementation detail abstracted behind a pluggable provider; the API contract (response shape) must remain stable regardless of which provider (mock, in-memory, or external) supplies the data.
- Temperature values and units must be clearly and consistently represented in the response (documented unit of measure).

### Security & Access Control
- This is a read-only, non-sensitive data endpoint; it does not require authentication or authorization unless the project's global API security policy mandates authentication for all endpoints (in which case standard JWT validation rules apply, returning HTTP `401 Unauthorized` for missing/invalid tokens).
- No user-specific or sensitive data is exposed by this endpoint.

### Performance & Green Code
- The endpoint returns a single forecast object per request (not a collection), so no pagination is required.
- The endpoint must not perform unnecessary repeated calls to an external weather data source within a single request lifecycle (avoid redundant over-fetching).
- Error responses must not leak internal exception details, stack traces, or provider-specific diagnostic information to the consumer.

## Prerequisites

None — this is a new, self-contained, read-only feature with no dependency on existing persisted domain entities or prior user stories.
