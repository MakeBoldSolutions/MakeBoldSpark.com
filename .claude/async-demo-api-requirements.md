# AsyncDemo API — Requirements Document

> **Purpose:** Full specification for reimplementing the AsyncSpark async/concurrency/weather/mock APIs in the ApiSpark project.
> **Source repo:** `c:\GitHub\MarkHazleton\AsyncSpark`
> **Target repo:** `c:\GitHub\MarkHazleton\ApiSpark`
> **Date:** 2026-05-17

---

## Table of Contents

1. [Overview](#overview)
2. [Target Architecture](#target-architecture)
3. [Shared Models](#shared-models)
4. [Feature: Async Basics — Weather Patterns](#feature-async-basics--weather-patterns)
5. [Feature: Cancellation Patterns](#feature-cancellation-patterns)
6. [Feature: Concurrency & Parallelism](#feature-concurrency--parallelism)
7. [Feature: Resilience — Remote Mock](#feature-resilience--remote-mock)
8. [Feature: Application Status](#feature-application-status)
9. [Supporting Services](#supporting-services)
10. [Configuration](#configuration)
11. [Middleware](#middleware)
12. [OpenAPI / Scalar Documentation](#openapi--scalar-documentation)
13. [NuGet Dependencies](#nuget-dependencies)
14. [Non-Functional Requirements](#non-functional-requirements)
15. [Out of Scope](#out-of-scope)

---

## Overview

The AsyncSpark project is an ASP.NET Core 10 educational web application demonstrating async/await, cancellation, concurrency, and resilience patterns. The web-facing UI (Razor views, Bootswatch themes, Markdown pages) stays in AsyncSpark. Only the **API surface** moves to ApiSpark as a new feature group.

In ApiSpark, these endpoints will live under a new route group `/api/async-demo`, following the existing minimal-API / feature-folder pattern already in use. No authentication is required (matching the source — all endpoints are anonymous).

---

## Target Architecture

### File layout (new files to create)

```
src/ApiSpark.Api/
  Features/
    AsyncDemo/
      Models/
        AsyncDemoModels.cs          ← all DTOs for this feature group
      WeatherPatterns/
        WeatherPatternsEndpoints.cs
        WeatherPatternsService.cs
      CancellationPatterns/
        CancellationPatternsEndpoints.cs
        CancellationPatternsService.cs
      ConcurrencyPatterns/
        ConcurrencyPatternsEndpoints.cs
        ConcurrencyPatternsService.cs
      RemoteMock/
        RemoteMockEndpoints.cs
        RemoteMockService.cs
      Status/
        AsyncStatusEndpoints.cs
```

### Route group registration in Program.cs

```csharp
var asyncDemoApi = app.MapGroup("/api/async-demo");
asyncDemoApi.MapWeatherPatternsApi();
asyncDemoApi.MapCancellationPatternsApi();
asyncDemoApi.MapConcurrencyPatternsApi();
asyncDemoApi.MapRemoteMockApi();
asyncDemoApi.MapAsyncStatusApi();
```

---

## Shared Models

File: `Features/AsyncDemo/Models/AsyncDemoModels.cs`

### MockResults

```csharp
public class MockResults
{
    public int LoopCount { get; set; }       // number of iterations requested
    public int MaxTimeMS { get; set; }       // max allowed time in milliseconds
    public long? RunTimeMS { get; set; }     // actual elapsed time
    public string? Message { get; set; }     // e.g. "Task Complete", "Time Out Occurred"
    public string? ResultValue { get; set; } // computed result or error code
}
```

### CurrentWeather

```csharp
public class CurrentWeather
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime FetchTime { get; set; }
    public DateTime ObservationTime { get; set; }
    public DateTime ObservationTimeUtc { get; set; }
    public TimeSpan ObservationAge => DateTime.Now - FetchTime;
    public LocationData? Location { get; set; }
    public WeatherData? CurrentConditions { get; set; }

    public class LocationData
    {
        public string? Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class WeatherData
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public double WindSpeed { get; set; }
        public string? WindDirection { get; set; }
        public double WindDirectionDegrees { get; set; }
        public string? Conditions { get; set; }
        public string? ConditionsDescription { get; set; }
        public double CloudCover { get; set; }
        public double Visibility { get; set; }
        public double RainfallOneHour { get; set; }
    }
}
```

### ApplicationStatus (subset needed for status endpoint)

```csharp
public sealed class ApplicationStatus
{
    public DateTime BuildDate { get; }
    public BuildVersion? BuildVersion { get; }
    public Dictionary<string, string> Features { get; }
    public List<string> Messages { get; }
    public string? Region { get; }
    public ServiceStatus Status { get; }
}

public sealed class BuildVersion
{
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public int Build { get; set; }
    public int Revision { get; set; }
    public override string ToString() => $"{MajorVersion}.{MinorVersion}.{Build}.{Revision}";
}

public enum ServiceStatus { Degraded, Offline, Online }
```

---

## Feature: Async Basics — Weather Patterns

**Route group prefix:** `/api/async-demo/weather`
**Tag:** `"Async Demo: Weather Patterns"`
**Source:** `AsyncSpark.Web/Controllers/Api/WeatherPatternsController.cs`

### External dependency

The weather endpoints call the **OpenWeatherMap** free-tier REST API:

- Base URL: `http://api.openweathermap.org`
- Current weather: `GET /data/2.5/weather?q={location}&units=imperial&appid={apiKey}`
- API key stored in user secrets or environment variable: `OpenWeatherMapApiKey`

The service should use `IHttpClientFactory` with a named client `"weather"` (30-second default timeout). Response deserialisation uses `System.Text.Json`. Cache current weather for 90 minutes using `IMemoryCache`.

### Endpoints

#### GET `/api/async-demo/weather/slow`

Demonstrates basic async/await **without** timeout protection — intentional "anti-pattern" for educational purposes.

| Parameter | Type | Source | Default | Notes |
|---|---|---|---|---|
| `location` | string | query | `"Dallas"` | City name |

**Behaviour:**
- Calls OpenWeatherMap current-weather endpoint.
- No timeout protection; the call blocks until the remote responds or the HTTP client times out.
- On success returns 200 with `CurrentWeather`.
- On error returns 500 with `{ error: string }`.
- Validates `location` is non-empty; returns 400 `{ error: "Location is required." }` if blank.

**Responses:**

| Status | Body |
|---|---|
| 200 | `CurrentWeather` |
| 400 | `{ "error": "Location is required." }` |
| 500 | `{ "error": string }` |

---

#### GET `/api/async-demo/weather/with-timeout`

Demonstrates async with **timeout and cancellation** protection.

| Parameter | Type | Source | Default |
|---|---|---|---|
| `location` | string | query | `"Dallas"` |
| `timeoutSeconds` | int | query | `5` |
| `cancellationToken` | CancellationToken | framework | — |

**Behaviour:**
- Creates a `CancellationTokenSource` with the given `timeoutSeconds`.
- Links it with the framework-provided `cancellationToken` via `CancellationTokenSource.CreateLinkedTokenSource`.
- Calls the weather service; passes the linked token.
- On timeout (`OperationCanceledException` when timeout token fires): returns 408.
- On client disconnect (`cancellationToken.IsCancellationRequested`): returns 499.

**Responses:**

| Status | Body |
|---|---|
| 200 | `CurrentWeather` |
| 400 | `{ "error": string }` |
| 408 | `{ "error": "Request timed out after {n} seconds.", "timeoutSeconds": int }` |
| 499 | `{ "error": "Request cancelled by client." }` |
| 500 | `{ "error": string }` |

---

#### GET `/api/async-demo/weather/with-retry`

Demonstrates **Polly retry policy** with exponential backoff and jitter.

| Parameter | Type | Source | Default |
|---|---|---|---|
| `location` | string | query | `"Dallas"` |
| `maxRetries` | int | query | `3` |
| `cancellationToken` | CancellationToken | framework | — |

**Behaviour:**
- Builds a Polly `AsyncRetryPolicy` at request time:
  ```csharp
  Policy.Handle<Exception>(ex => ex is not OperationCanceledException)
        .WaitAndRetryAsync(
            maxRetries,
            attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
                       + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)),
            onRetry: (exception, delay, attempt, _) => logger.LogWarning(...)
        )
  ```
- Captures each retry attempt in a list for the response.
- Returns weather data plus a `retryInfo` object.

**Response body (200):**
```json
{
  "weather": { /* CurrentWeather */ },
  "retryInfo": {
    "attemptsUsed": 1,
    "maxRetries": 3,
    "retryDelays": [/* ms per attempt */]
  }
}
```

**Responses:**

| Status | Body |
|---|---|
| 200 | `{ weather, retryInfo }` |
| 400 | `{ "error": string }` |
| 408 | `{ "error": "Request timed out after retries." }` |
| 500 | `{ "error": string }` |

---

#### GET `/api/async-demo/weather/multiple`

Demonstrates **parallel weather fetching** using `Task.WhenAll`.

| Parameter | Type | Source | Default |
|---|---|---|---|
| `locations` | string | query | `"Dallas,London,Tokyo"` |
| `cancellationToken` | CancellationToken | framework | — |

**Behaviour:**
- Splits `locations` on commas; rejects empty or >10 locations (returns 400).
- Creates one `GetCurrentWeatherAsync` task per location; awaits `Task.WhenAll(tasks)`.
- Records total elapsed milliseconds and per-city result.
- A per-city failure is captured inline (`Success = false`, `ErrorMessage` set) and does not fail the whole request.

**Response body (200):**
```json
{
  "summary": {
    "totalCities": 3,
    "successCount": 3,
    "failureCount": 0,
    "totalElapsedMilliseconds": 412
  },
  "results": [ /* CurrentWeather[] */ ]
}
```

**Responses:**

| Status | Body |
|---|---|
| 200 | `{ summary, results }` |
| 400 | `{ "error": string }` |
| 408 | `{ "error": string }` |
| 500 | `{ "error": string }` |

---

## Feature: Cancellation Patterns

**Route group prefix:** `/api/async-demo/cancellation`
**Tag:** `"Async Demo: Cancellation Patterns"`
**Source:** `AsyncSpark.Web/Controllers/Api/CancellationPatternsController.cs`

The endpoints share a private CPU-bound loop helper that computes a decimal accumulator:

```csharp
static async Task<decimal> RunLoopAsync(int iterations, CancellationToken ct = default)
{
    decimal result = 0;
    await Task.Run(async () =>
    {
        for (int i = 0; i < iterations; i++)
        {
            ct.ThrowIfCancellationRequested();
            result += i * 0.001m;
            await Task.Delay(1, ct);
        }
    }, ct);
    return result;
}
```

### Endpoints

#### GET `/api/async-demo/cancellation/no-cancellation`

Anti-pattern: long-running operation **without** cancellation support.

| Parameter | Type | Source | Default |
|---|---|---|---|
| `iterations` | int | query | `100` |

**Behaviour:** Runs the loop with `CancellationToken.None`. Even if the client disconnects, the server keeps running. Returns result on completion.

**Response body (200):**
```json
{
  "result": 4950.000,
  "iterations": 100,
  "elapsedMilliseconds": 312,
  "cancellable": false
}
```

---

#### GET `/api/async-demo/cancellation/with-token`

Best practice: honours the **framework-wired cancellation token**.

| Parameter | Type | Source | Default |
|---|---|---|---|
| `iterations` | int | query | `100` |
| `cancellationToken` | CancellationToken | framework | — |

**Behaviour:** Passes `cancellationToken` directly to the loop. If the client disconnects, `OperationCanceledException` is caught and 499 is returned.

**Responses:**

| Status | Body |
|---|---|
| 200 | `{ "result": decimal, "elapsedMilliseconds": long, "cancellable": true }` |
| 499 | `{ "error": "Request cancelled by client." }` |
| 500 | `{ "error": string }` |

---

#### GET `/api/async-demo/cancellation/with-timeout`

Demonstrates **linked tokens**: timeout + client cancellation.

| Parameter | Type | Source | Default |
|---|---|---|---|
| `iterations` | int | query | `100` |
| `timeoutSeconds` | int | query | `5` |
| `cancellationToken` | CancellationToken | framework | — |

**Behaviour:**
- Creates `CancellationTokenSource` with `TimeSpan.FromSeconds(timeoutSeconds)`.
- Links with the framework token.
- Distinguishes between timeout (408) and client disconnect (499) by checking which token cancelled.

**Responses:**

| Status | Body |
|---|---|
| 200 | `{ "result": decimal, "iterations": int, "timeoutSeconds": int, "elapsedMilliseconds": long }` |
| 408 | `{ "error": "Request timed out after {n} seconds.", "timeoutSeconds": int }` |
| 499 | `{ "error": "Request cancelled by client." }` |
| 500 | `{ "error": string }` |

---

#### GET `/api/async-demo/cancellation/with-cleanup`

Demonstrates **resource cleanup** via `finally` blocks that execute even after cancellation.

| Parameter | Type | Source | Default |
|---|---|---|---|
| `iterations` | int | query | `100` |
| `cancellationToken` | CancellationToken | framework | — |

**Behaviour:**
- Runs the loop inside a `try/catch/finally`.
- The `finally` block runs a short cleanup step regardless of cancellation.
- Records whether cleanup ran and how long it took.

**Responses:**

| Status | Body |
|---|---|
| 200 | `{ "result": decimal, "elapsedMilliseconds": long, "cleanupRan": true, "cleanupElapsedMilliseconds": long }` |
| 499 | `{ "error": "Request cancelled by client.", "cleanupRan": true, "cleanupElapsedMilliseconds": long }` |
| 500 | `{ "error": string }` |

---

## Feature: Concurrency & Parallelism

**Route group prefix:** `/api/async-demo/concurrency`
**Tag:** `"Async Demo: Concurrency & Parallelism"`
**Source:** `AsyncSpark.Web/Controllers/Api/ConcurrencyPatternsController.cs`

Each endpoint simulates independent async operations. A single simulated operation performs `iterationsPerOperation` iterations of `await Task.Delay(1)` plus arithmetic, returning elapsed time.

### Endpoints

#### GET `/api/async-demo/concurrency/sequential`

Runs operations one after another (anti-pattern baseline).

| Parameter | Type | Source | Default |
|---|---|---|---|
| `operationCount` | int | query | `5` |
| `iterationsPerOperation` | int | query | `50` |

**Response body (200):**
```json
{
  "executionMode": "Sequential",
  "totalOperations": 5,
  "totalElapsedMilliseconds": 1250,
  "averageMillisecondsPerOperation": 250,
  "results": [
    { "operationId": 1, "elapsedMilliseconds": 251, "result": 1225.0 }
  ]
}
```

---

#### GET `/api/async-demo/concurrency/parallel`

Runs all operations concurrently via `Task.WhenAll`.

| Parameter | Type | Source | Default |
|---|---|---|---|
| `operationCount` | int | query | `5` |
| `iterationsPerOperation` | int | query | `50` |

**Response body (200):**
```json
{
  "executionMode": "Parallel",
  "totalOperations": 5,
  "totalElapsedMilliseconds": 262,
  "speedupFactor": 4.77,
  "results": [ /* same shape as sequential */ ]
}
```

---

#### GET `/api/async-demo/concurrency/throttled`

Uses `SemaphoreSlim` to limit concurrent operations.

| Parameter | Type | Source | Default |
|---|---|---|---|
| `operationCount` | int | query | `10` |
| `maxConcurrency` | int | query | `3` |
| `iterationsPerOperation` | int | query | `50` |

**Behaviour:**
- Creates a `SemaphoreSlim(maxConcurrency)`.
- Each operation acquires the semaphore before running and releases in `finally`.
- Results include a `wave` number (which batch of `maxConcurrency` an operation ran in).

**Response body (200):**
```json
{
  "executionMode": "Throttled",
  "maxConcurrency": 3,
  "totalOperations": 10,
  "totalElapsedMilliseconds": 890,
  "results": [
    { "operationId": 1, "wave": 1, "elapsedMilliseconds": 265, "result": 1225.0 }
  ]
}
```

---

#### GET `/api/async-demo/concurrency/comparison`

Runs all three modes and returns side-by-side metrics.

| Parameter | Type | Source | Default |
|---|---|---|---|
| `operationCount` | int | query | `5` |
| `maxConcurrency` | int | query | `2` |
| `iterationsPerOperation` | int | query | `50` |

**Response body (200):**
```json
{
  "operationCount": 5,
  "iterationsPerOperation": 50,
  "maxConcurrency": 2,
  "sequential": { "totalElapsedMilliseconds": 1250 },
  "parallel":   { "totalElapsedMilliseconds": 260, "speedupVsSequential": 4.8 },
  "throttled":  { "totalElapsedMilliseconds": 540, "speedupVsSequential": 2.3 }
}
```

---

## Feature: Resilience — Remote Mock

**Route group prefix:** `/api/async-demo/remote`
**Tag:** `"Async Demo: Resilience & Timeouts"`
**Source:** `AsyncSpark.Web/Controllers/Api/RemoteController.cs`

This endpoint simulates a slow or timing-out downstream server. The PollyController in AsyncSpark calls this endpoint to demonstrate retry logic; in ApiSpark, both the caller (demo page) and this mock endpoint can coexist.

### Endpoints

#### POST `/api/async-demo/remote/results`

Runs a configurable loop then returns; may intentionally time out.

**Request body** (`application/json`):
```json
{
  "loopCount": 5,
  "maxTimeMS": 1000
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `loopCount` | int | yes | Number of loop iterations (each sleeps ~10ms) |
| `maxTimeMS` | int | yes | Internal timeout budget in milliseconds |

**Behaviour:**
- Creates a `CancellationTokenSource` with `maxTimeMS` timeout.
- Runs `loopCount` iterations of a compute loop (similar to `LongRunningOperation`).
- If the internal timeout fires before the loop completes, sets `Message = "Time Out Occurred"` and `ResultValue = "408"`.
- If loop completes, sets `Message = "Task Complete"`, `ResultValue = computed decimal as string`.
- Records `RunTimeMS`.
- Uses `IMemoryCache` to cache repeated identical requests for 30 seconds (key: `$"remote_{loopCount}_{maxTimeMS}"`).

**Responses:**

| Status | Body |
|---|---|
| 200 | `MockResults` |
| 408 | `MockResults` (with `Message = "Time Out Occurred"`) |
| 500 | `{ "error": string }` |

---

## Feature: Application Status

**Route group prefix:** `/api/async-demo/status`
**Tag:** `"Async Demo: Monitoring & Health"`
**Source:** `AsyncSpark.Web/Controllers/Api/StatusController.cs`

### Endpoints

#### GET `/api/async-demo/status`

Returns the current application status. Result is cached in `IMemoryCache` for 24 hours under key `"ApplicationStatus"`.

**Behaviour:**
- On cache miss: constructs `ApplicationStatus` from `Assembly.GetExecutingAssembly()` and assembly version metadata.
- Reads `Region` from `IConfiguration["Region"]` or `IConfiguration["WEBSITE_SITE_NAME"]`.
- `Status` defaults to `ServiceStatus.Online`.
- Sets `Features` and `Messages` as empty collections (can be extended).

**Response body (200):**
```json
{
  "buildDate": "2026-05-17T00:00:00Z",
  "buildVersion": { "majorVersion": 1, "minorVersion": 0, "build": 0, "revision": 0 },
  "features": {},
  "messages": [],
  "region": "local",
  "status": "Online"
}
```

---

#### GET `/api/async-demo/status/appsettings`

Tests `IConfiguration` helper methods for CSV parsing. Useful for debugging config pipeline.

**Behaviour:**
- Reads `Async:TestIds` (expected comma-separated ints), `Async:TestId` (single int), `Async:TestNames` (comma-separated strings), `Async:TestName` (single string).

**Response body (200):**
```json
{
  "testIds": [1, 2, 3, 4, 5],
  "testId": 1,
  "testNames": ["Mark", "Bob", "Sam"],
  "testName": "Mark"
}
```

---

## Supporting Services

### IWeatherService / OpenWeatherMapWeatherService

Implement as a scoped service in the `AsyncDemo` feature:

```csharp
public interface IAsyncDemoWeatherService
{
    Task<CurrentWeather> GetCurrentWeatherAsync(string location, CancellationToken ct = default);
}
```

**Implementation details:**
- Named `HttpClient` `"weather"` via `IHttpClientFactory`.
- Base address: `http://api.openweathermap.org`.
- Units: `imperial` (Fahrenheit).
- Deserialise `CurrentConditionsResponse` (OpenWeatherMap JSON schema) into `CurrentWeather`.
- Cache per `(location.ToLowerInvariant())` for 90 minutes in `IMemoryCache`.
- Log warning and return `CurrentWeather { Success = false, ErrorMessage = ... }` on any exception; do not propagate.

**OpenWeatherMap response mapping** (abbreviated):

| OWM field | `CurrentWeather` field |
|---|---|
| `main.temp` | `CurrentConditions.Temperature` |
| `main.humidity` | `CurrentConditions.Humidity` |
| `main.pressure` | `CurrentConditions.Pressure` |
| `wind.speed` | `CurrentConditions.WindSpeed` |
| `wind.deg` | `CurrentConditions.WindDirectionDegrees` + compass lookup |
| `weather[0].main` | `CurrentConditions.Conditions` |
| `weather[0].description` | `CurrentConditions.ConditionsDescription` |
| `clouds.all` | `CurrentConditions.CloudCover` |
| `visibility` | `CurrentConditions.Visibility` |
| `rain["1h"]` | `CurrentConditions.RainfallOneHour` |
| `dt` (unix) | `ObservationTime` (local) + `ObservationTimeUtc` |
| `name` | `Location.Name` |
| `coord.lat/lon` | `Location.Latitude/Longitude` |

### CompassDirection helper

Implement a static helper that converts a bearing in degrees to a 16-point compass abbreviation (N, NNE, NE, ENE, E, ESE, SE, SSE, S, SSW, SW, WSW, W, WNW, NW, NNW). Used for `CurrentConditions.WindDirection`.

### CancellationPatternsService

Stateless service class with one method:

```csharp
public static Task<decimal> RunLoopAsync(int iterations, CancellationToken ct)
```

### ConcurrencyPatternsService

Stateless service class with:

```csharp
public static Task<(long ElapsedMs, decimal Result)> RunOperationAsync(
    int operationId, int iterations, CancellationToken ct = default)
```

### RemoteMockService

Scoped service:

```csharp
public Task<MockResults> RunMockAsync(int loopCount, int maxTimeMS, CancellationToken ct = default)
```

Uses `IMemoryCache` for 30-second caching.

---

## Configuration

Add the following sections to `appsettings.json` in ApiSpark:

```json
{
  "OpenWeatherMapApiKey": "",
  "Async": {
    "TestIds": "1,2,3,4,5",
    "TestId": "1",
    "TestNames": "Mark,Bob,Sam",
    "TestName": "Mark"
  }
}
```

`OpenWeatherMapApiKey` should be set via **User Secrets** in development and via an environment variable in production. Log a warning at startup if the key is missing or equals the placeholder `"KEYMISSING"`.

### Named HttpClient registration

```csharp
builder.Services.AddHttpClient("weather", client =>
{
    client.BaseAddress = new Uri("http://api.openweathermap.org");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### IMemoryCache

Already registered by the existing ApiSpark `Program.cs` — verify `builder.Services.AddMemoryCache()` is present.

### Service registrations

```csharp
builder.Services.AddScoped<IAsyncDemoWeatherService, OpenWeatherMapWeatherService>();
builder.Services.AddScoped<RemoteMockService>();
```

---

## Middleware

### RequestLoggingMiddleware

ApiSpark already has `Infrastructure/Observability/RequestLoggingMiddleware.cs`. Verify it:
- Generates a unique request ID (Guid).
- Logs method + path + remote IP at start (Information).
- Logs status code + elapsed ms at completion.
- Adds `X-Request-ID` response header.
- Logs at Warning level for 4xx/5xx responses.

No additional middleware is needed.

---

## OpenAPI / Scalar Documentation

All new endpoints must include:

- `.WithName("OperationName")` — unique, PascalCase
- `.WithTags("Async Demo: <Category>")` — one of the five tag strings defined above
- `.WithSummary("...")` — one-line summary
- `.Produces<T>(200)` / `.Produces(408)` etc. for all response types
- `.AllowAnonymous()` — no auth required

The existing `MapScalarApiReference()` in `Program.cs` will automatically surface these endpoints. No changes to `CustomScalarExtensions` are required.

---

## NuGet Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Polly` | 8.x | Retry / timeout policies in `WeatherPatternsEndpoints` and `RemoteMockEndpoints` |
| `Microsoft.AspNetCore.OpenApi` | 10.x | Already present |
| `Scalar.AspNetCore` | 2.x | Already present |

Add `Polly` to `ApiSpark.Api.csproj`:

```xml
<PackageReference Include="Polly" Version="8.6.6" />
```

No other new packages are required. `IMemoryCache`, `IHttpClientFactory`, and `System.Text.Json` are provided by the ASP.NET Core framework.

---

## Non-Functional Requirements

| Requirement | Specification |
|---|---|
| Framework | .NET 10 / ASP.NET Core 10 minimal API |
| Authentication | None — all `/api/async-demo/*` endpoints are anonymous |
| Content-Type | `application/json` for all responses |
| Error format | `{ "error": "message" }` for all error responses |
| Caching | Weather: 90 min; Status: 24 h; Remote mock: 30 s — all via `IMemoryCache` |
| Logging | Use `ILogger<T>` injected into each service; log at Warning for retries, Error for unhandled exceptions |
| Thread safety | No shared mutable state; semaphore usage is per-request |
| Cancellation | All operations that can be cancelled must accept and honour `CancellationToken` |
| Status codes | Follow the source exactly: 200, 400, 408 (timeout), 499 (client disconnect), 500 |
| Tests | Add integration tests in `ApiSpark.Api.Tests` for at least: health, sequential, parallel, no-cancellation, remote-mock timeout |

---

## Out of Scope

The following from AsyncSpark are **not** being moved to ApiSpark:

- Razor Views / MVC controllers (`HomeController`, `OpenWeatherController`, `PollyController`, `BulkCallsController`)
- Bootswatch theme switcher (`WebSpark.Bootswatch`)
- Westwind Markdown rendering
- Session-based state
- `BulkCallsController` / `IHttpGetCallService` (HTTP bulk GET orchestration — UI-only demo)
- `AsyncSpark.Weather` forecast endpoints (not surfaced via API in the source)
- Security headers middleware (already handled in ApiSpark via CORS setup)
- `EncodingMiddleware` (MVC-specific, not needed in a pure API project)
- `ConfigurationValidationService` / `ConfigurationHealthCheck` as standalone services (validation logic can be inlined into startup)
