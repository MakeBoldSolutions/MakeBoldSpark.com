using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MakeBoldSpark.Api.Features.AsyncDemo.Models;
using MakeBoldSpark.Api.Infrastructure.OpenApi;

namespace MakeBoldSpark.Api.Features.AsyncDemo.CancellationPatterns;

public static class CancellationPatternsEndpoints
{
    public static RouteGroupBuilder MapCancellationPatternsApi(this RouteGroupBuilder group)
    {
        group.MapGet("/no-cancellation", async (
            [Range(1, 10000)]
            [Description("Number of CPU-bound loop iterations to execute.")]
            [DefaultValue(100)]
            int iterations = 100) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await CancellationPatternsService.RunLoopAsync(iterations, CancellationToken.None);
            sw.Stop();

            return Results.Ok(new
            {
                result,
                iterations,
                elapsedMilliseconds = sw.ElapsedMilliseconds,
                cancellable = false
            });
        })
        .WithName("CancellationNoCancellation")
        .WithTags(MakeBoldSparkOpenApiTags.AsyncCancellationPatterns)
        .WithSummary("Long-running operation without cancellation support (anti-pattern)")
        .Produces<CancellationLoopResponse>(200)
        .AllowAnonymous();

        group.MapGet("/with-token", async (
            CancellationToken cancellationToken,
            [Range(1, 10000)]
            [Description("Number of CPU-bound loop iterations to execute.")]
            [DefaultValue(100)]
            int iterations = 100) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await CancellationPatternsService.RunLoopAsync(iterations, cancellationToken);
                sw.Stop();
                return Results.Ok(new
                {
                    result,
                    elapsedMilliseconds = sw.ElapsedMilliseconds,
                    cancellable = true
                });
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
        .WithName("CancellationWithToken")
        .WithTags(MakeBoldSparkOpenApiTags.AsyncCancellationPatterns)
        .WithSummary("Long-running operation that honours the framework cancellation token")
        .Produces<CancellationLoopResponse>(200)
        .Produces(499)
        .Produces(500)
        .AllowAnonymous();

        group.MapGet("/with-timeout", async (
            CancellationToken cancellationToken,
            [Range(1, 10000)]
            [Description("Number of CPU-bound loop iterations to execute.")]
            [DefaultValue(100)]
            int iterations = 100,
            [Range(1, 60)]
            [Description("Timeout in seconds for the linked cancellation token.")]
            [DefaultValue(5)]
            int timeoutSeconds = 5) =>
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await CancellationPatternsService.RunLoopAsync(iterations, linkedCts.Token);
                sw.Stop();
                return Results.Ok(new
                {
                    result,
                    iterations,
                    timeoutSeconds,
                    elapsedMilliseconds = sw.ElapsedMilliseconds
                });
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
        .WithName("CancellationWithTimeout")
        .WithTags(MakeBoldSparkOpenApiTags.AsyncCancellationPatterns)
        .WithSummary("Linked tokens: timeout + client cancellation with status code differentiation")
        .Produces<CancellationLoopResponse>(200)
        .Produces(408)
        .Produces(499)
        .Produces(500)
        .AllowAnonymous();

        group.MapGet("/with-cleanup", async (
            CancellationToken cancellationToken,
            [Range(1, 10000)]
            [Description("Number of CPU-bound loop iterations to execute.")]
            [DefaultValue(100)]
            int iterations = 100) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool cleanupRan = false;
            long cleanupElapsedMs = 0;
            decimal result = 0;
            bool cancelled = false;

            try
            {
                result = await CancellationPatternsService.RunLoopAsync(iterations, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            finally
            {
                var cleanupSw = System.Diagnostics.Stopwatch.StartNew();
                await Task.Delay(10);
                cleanupSw.Stop();
                cleanupRan = true;
                cleanupElapsedMs = cleanupSw.ElapsedMilliseconds;
            }
            sw.Stop();

            if (cancelled)
            {
                return Results.Json(new
                {
                    error = "Request cancelled by client.",
                    cleanupRan,
                    cleanupElapsedMilliseconds = cleanupElapsedMs
                }, statusCode: 499);
            }

            return Results.Ok(new
            {
                result,
                elapsedMilliseconds = sw.ElapsedMilliseconds,
                cleanupRan,
                cleanupElapsedMilliseconds = cleanupElapsedMs
            });
        })
        .WithName("CancellationWithCleanup")
        .WithTags(MakeBoldSparkOpenApiTags.AsyncCancellationPatterns)
        .WithSummary("Resource cleanup via finally blocks that run even after cancellation")
        .Produces<CancellationCleanupResponse>(200)
        .Produces(499)
        .Produces(500)
        .AllowAnonymous();

        return group;
    }
}
