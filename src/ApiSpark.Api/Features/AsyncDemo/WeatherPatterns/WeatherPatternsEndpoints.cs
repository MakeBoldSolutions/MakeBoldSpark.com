using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ApiSpark.Api.Features.AsyncDemo.Models;
using ApiSpark.Api.Infrastructure.OpenApi;
using Polly;

namespace ApiSpark.Api.Features.AsyncDemo.WeatherPatterns;

public static class WeatherPatternsEndpoints
{
    public static RouteGroupBuilder MapWeatherPatternsApi(this RouteGroupBuilder group)
    {
        group.MapGet("/slow", async (
            [Description("City name to fetch from OpenWeatherMap.")]
            [DefaultValue("Dallas")]
            string location = "Dallas",
            IAsyncDemoWeatherService weatherService = null!) =>
        {
            if (string.IsNullOrWhiteSpace(location))
                return Results.BadRequest(new { error = "Location is required." });

            try
            {
                var weather = await weatherService.GetCurrentWeatherAsync(location);
                return Results.Ok(weather);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        })
        .WithName("GetWeatherSlow")
        .WithTags(ApiSparkOpenApiTags.AsyncWeatherPatterns)
        .WithSummary("Fetch weather without timeout protection (anti-pattern demo)")
        .Produces<CurrentWeather>(200)
        .Produces(400)
        .Produces(500)
        .AllowAnonymous();

        group.MapGet("/with-timeout", async (
            CancellationToken cancellationToken,
            IAsyncDemoWeatherService weatherService,
            [Description("City name to fetch from OpenWeatherMap.")]
            [DefaultValue("Dallas")]
            string location = "Dallas",
            [Range(1, 60)]
            [Description("Maximum time to wait before returning HTTP 408.")]
            [DefaultValue(5)]
            int timeoutSeconds = 5) =>
        {
            if (string.IsNullOrWhiteSpace(location))
                return Results.BadRequest(new { error = "Location is required." });

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                var weather = await weatherService.GetCurrentWeatherAsync(location, linkedCts.Token);
                return Results.Ok(weather);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                return Results.Json(new { error = $"Request timed out after {timeoutSeconds} seconds.", timeoutSeconds }, statusCode: 408);
            }
            catch (OperationCanceledException)
            {
                return Results.Json(new { error = "Request cancelled by client." }, statusCode: 499);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        })
        .WithName("GetWeatherWithTimeout")
        .WithTags(ApiSparkOpenApiTags.AsyncWeatherPatterns)
        .WithSummary("Fetch weather with timeout and cancellation protection")
        .Produces<CurrentWeather>(200)
        .Produces(400)
        .Produces(408)
        .Produces(499)
        .Produces(500)
        .AllowAnonymous();

        group.MapGet("/with-retry", async (
            CancellationToken cancellationToken,
            IAsyncDemoWeatherService weatherService,
            ILogger<Program> logger,
            [Description("City name to fetch from OpenWeatherMap.")]
            [DefaultValue("Dallas")]
            string location = "Dallas",
            [Range(0, 10)]
            [Description("Number of retry attempts before surfacing the downstream error.")]
            [DefaultValue(3)]
            int maxRetries = 3) =>
        {
            if (string.IsNullOrWhiteSpace(location))
                return Results.BadRequest(new { error = "Location is required." });

            var retryDelays = new List<double>();
            var attemptsUsed = 0;

            var retryPolicy = Policy
                .Handle<Exception>(ex => ex is not OperationCanceledException)
                .WaitAndRetryAsync(
                    maxRetries,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
                               + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)),
                    onRetry: (exception, delay, attempt, _) =>
                    {
                        retryDelays.Add(delay.TotalMilliseconds);
                        attemptsUsed = attempt;
                        logger.LogWarning(exception, "Weather retry {Attempt} after {Delay}ms for {Location}", attempt, delay.TotalMilliseconds, location);
                    });

            try
            {
                CurrentWeather? weather = null;
                await retryPolicy.ExecuteAsync(async () =>
                {
                    weather = await weatherService.GetCurrentWeatherAsync(location, cancellationToken);
                });

                attemptsUsed = Math.Max(1, attemptsUsed);

                return Results.Ok(new
                {
                    weather,
                    retryInfo = new
                    {
                        attemptsUsed,
                        maxRetries,
                        retryDelays
                    }
                });
            }
            catch (OperationCanceledException)
            {
                return Results.Json(new { error = "Request timed out after retries." }, statusCode: 408);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        })
        .WithName("GetWeatherWithRetry")
        .WithTags(ApiSparkOpenApiTags.AsyncWeatherPatterns)
        .WithSummary("Fetch weather with Polly retry and exponential backoff")
        .Produces<WeatherRetryResponse>(200)
        .Produces(400)
        .Produces(408)
        .Produces(500)
        .AllowAnonymous();

        group.MapGet("/multiple", async (
            CancellationToken cancellationToken,
            IAsyncDemoWeatherService weatherService,
            [Description("Comma-separated city list. Maximum 10 cities.")]
            [DefaultValue("Dallas,London,Tokyo")]
            string locations = "Dallas,London,Tokyo") =>
        {
            var locationList = locations.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (locationList.Length == 0)
                return Results.BadRequest(new { error = "At least one location is required." });
            if (locationList.Length > 10)
                return Results.BadRequest(new { error = "Maximum 10 locations allowed." });

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tasks = locationList.Select(loc =>
                weatherService.GetCurrentWeatherAsync(loc, cancellationToken)).ToArray();

            CurrentWeather[] results;
            try
            {
                results = await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                return Results.Json(new { error = "Request cancelled." }, statusCode: 408);
            }
            sw.Stop();

            var successCount = results.Count(r => r.Success);

            return Results.Ok(new
            {
                summary = new
                {
                    totalCities = results.Length,
                    successCount,
                    failureCount = results.Length - successCount,
                    totalElapsedMilliseconds = sw.ElapsedMilliseconds
                },
                results
            });
        })
        .WithName("GetWeatherMultiple")
        .WithTags(ApiSparkOpenApiTags.AsyncWeatherPatterns)
        .WithSummary("Fetch weather for multiple cities in parallel using Task.WhenAll")
        .Produces<MultiWeatherResponse>(200)
        .Produces(400)
        .Produces(408)
        .Produces(500)
        .AllowAnonymous();

        return group;
    }
}
