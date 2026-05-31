using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ApiSpark.Api.Features.AsyncDemo.Models;
using ApiSpark.Api.Infrastructure.OpenApi;

namespace ApiSpark.Api.Features.AsyncDemo.ConcurrencyPatterns;

public static class ConcurrencyPatternsEndpoints
{
    public static RouteGroupBuilder MapConcurrencyPatternsApi(this RouteGroupBuilder group)
    {
        group.MapGet("/sequential", async (
            [Range(1, 50)]
            [Description("Number of operations to execute one after another.")]
            [DefaultValue(5)]
            int operationCount = 5,
            [Range(1, 10000)]
            [Description("Number of CPU-bound loop iterations per operation.")]
            [DefaultValue(50)]
            int iterationsPerOperation = 50) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var results = new List<object>();

            for (int i = 1; i <= operationCount; i++)
            {
                var (elapsed, result) = await ConcurrencyPatternsService.RunOperationAsync(i, iterationsPerOperation);
                results.Add(new { operationId = i, elapsedMilliseconds = elapsed, result });
            }
            sw.Stop();

            return Results.Ok(new
            {
                executionMode = "Sequential",
                totalOperations = operationCount,
                totalElapsedMilliseconds = sw.ElapsedMilliseconds,
                averageMillisecondsPerOperation = operationCount > 0 ? sw.ElapsedMilliseconds / operationCount : 0,
                results
            });
        })
        .WithName("ConcurrencySequential")
        .WithTags(ApiSparkOpenApiTags.AsyncConcurrencyPatterns)
        .WithSummary("Sequential operation execution (anti-pattern baseline)")
        .Produces<ConcurrencyRunResponse>(200)
        .AllowAnonymous();

        group.MapGet("/parallel", async (
            [Range(1, 50)]
            [Description("Number of operations to start concurrently.")]
            [DefaultValue(5)]
            int operationCount = 5,
            [Range(1, 10000)]
            [Description("Number of CPU-bound loop iterations per operation.")]
            [DefaultValue(50)]
            int iterationsPerOperation = 50) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var tasks = Enumerable.Range(1, operationCount)
                .Select(i => ConcurrencyPatternsService.RunOperationAsync(i, iterationsPerOperation))
                .ToArray();

            var rawResults = await Task.WhenAll(tasks);
            sw.Stop();

            var results = rawResults.Select((r, i) => new
            {
                operationId = i + 1,
                elapsedMilliseconds = r.ElapsedMs,
                result = r.Result
            }).ToList();

            var sequentialEstimate = rawResults.Sum(r => r.ElapsedMs);
            var speedupFactor = sequentialEstimate > 0
                ? Math.Round((double)sequentialEstimate / sw.ElapsedMilliseconds, 2)
                : 1.0;

            return Results.Ok(new
            {
                executionMode = "Parallel",
                totalOperations = operationCount,
                totalElapsedMilliseconds = sw.ElapsedMilliseconds,
                speedupFactor,
                results
            });
        })
        .WithName("ConcurrencyParallel")
        .WithTags(ApiSparkOpenApiTags.AsyncConcurrencyPatterns)
        .WithSummary("All operations concurrently via Task.WhenAll")
        .Produces<ParallelConcurrencyResponse>(200)
        .AllowAnonymous();

        group.MapGet("/throttled", async (
            [Range(1, 50)]
            [Description("Number of operations to enqueue.")]
            [DefaultValue(10)]
            int operationCount = 10,
            [Range(1, 20)]
            [Description("Maximum number of operations allowed to run simultaneously.")]
            [DefaultValue(3)]
            int maxConcurrency = 3,
            [Range(1, 10000)]
            [Description("Number of CPU-bound loop iterations per operation.")]
            [DefaultValue(50)]
            int iterationsPerOperation = 50) =>
        {
            var semaphore = new SemaphoreSlim(maxConcurrency);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var waveCounter = 0;
            var resultsLock = new object();
            var results = new List<object>();

            var tasks = Enumerable.Range(1, operationCount).Select(async i =>
            {
                await semaphore.WaitAsync();
                int wave;
                lock (resultsLock) { wave = ++waveCounter / maxConcurrency + 1; }
                try
                {
                    var (elapsed, result) = await ConcurrencyPatternsService.RunOperationAsync(i, iterationsPerOperation);
                    lock (resultsLock)
                    {
                        results.Add(new { operationId = i, wave, elapsedMilliseconds = elapsed, result });
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            sw.Stop();

            var orderedResults = results
                .Cast<dynamic>()
                .OrderBy(r => (int)r.operationId)
                .ToList();

            return Results.Ok(new
            {
                executionMode = "Throttled",
                maxConcurrency,
                totalOperations = operationCount,
                totalElapsedMilliseconds = sw.ElapsedMilliseconds,
                results = orderedResults
            });
        })
        .WithName("ConcurrencyThrottled")
        .WithTags(ApiSparkOpenApiTags.AsyncConcurrencyPatterns)
        .WithSummary("Throttled concurrency using SemaphoreSlim")
        .Produces<ConcurrencyRunResponse>(200)
        .AllowAnonymous();

        group.MapGet("/comparison", async (
            [Range(1, 50)]
            [Description("Number of operations to execute in each comparison mode.")]
            [DefaultValue(5)]
            int operationCount = 5,
            [Range(1, 20)]
            [Description("Maximum concurrency used by the throttled branch.")]
            [DefaultValue(2)]
            int maxConcurrency = 2,
            [Range(1, 10000)]
            [Description("Number of CPU-bound loop iterations per operation.")]
            [DefaultValue(50)]
            int iterationsPerOperation = 50) =>
        {
            // Sequential
            var seqSw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 1; i <= operationCount; i++)
                await ConcurrencyPatternsService.RunOperationAsync(i, iterationsPerOperation);
            seqSw.Stop();

            // Parallel
            var parSw = System.Diagnostics.Stopwatch.StartNew();
            var parTasks = Enumerable.Range(1, operationCount)
                .Select(i => ConcurrencyPatternsService.RunOperationAsync(i, iterationsPerOperation))
                .ToArray();
            await Task.WhenAll(parTasks);
            parSw.Stop();

            // Throttled
            var throttleSw = System.Diagnostics.Stopwatch.StartNew();
            var semaphore = new SemaphoreSlim(maxConcurrency);
            var throttleTasks = Enumerable.Range(1, operationCount).Select(async i =>
            {
                await semaphore.WaitAsync();
                try { await ConcurrencyPatternsService.RunOperationAsync(i, iterationsPerOperation); }
                finally { semaphore.Release(); }
            });
            await Task.WhenAll(throttleTasks);
            throttleSw.Stop();

            double seqMs = seqSw.ElapsedMilliseconds;
            double parMs = parSw.ElapsedMilliseconds;
            double thrMs = throttleSw.ElapsedMilliseconds;

            return Results.Ok(new
            {
                operationCount,
                iterationsPerOperation,
                maxConcurrency,
                sequential = new { totalElapsedMilliseconds = (long)seqMs },
                parallel = new
                {
                    totalElapsedMilliseconds = (long)parMs,
                    speedupVsSequential = parMs > 0 ? Math.Round(seqMs / parMs, 2) : 0.0
                },
                throttled = new
                {
                    totalElapsedMilliseconds = (long)thrMs,
                    speedupVsSequential = thrMs > 0 ? Math.Round(seqMs / thrMs, 2) : 0.0
                }
            });
        })
        .WithName("ConcurrencyComparison")
        .WithTags(ApiSparkOpenApiTags.AsyncConcurrencyPatterns)
        .WithSummary("Side-by-side comparison of sequential, parallel, and throttled execution")
        .Produces<ConcurrencyComparisonResponse>(200)
        .AllowAnonymous();

        return group;
    }
}
