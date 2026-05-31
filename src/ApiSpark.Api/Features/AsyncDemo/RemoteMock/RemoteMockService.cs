using ApiSpark.Api.Features.AsyncDemo.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ApiSpark.Api.Features.AsyncDemo.RemoteMock;

public class RemoteMockService(IMemoryCache cache, ILogger<RemoteMockService> logger)
{
    public async Task<MockResults> RunMockAsync(int loopCount, int maxTimeMS, CancellationToken ct = default)
    {
        var cacheKey = $"remote_{loopCount}_{maxTimeMS}";
        if (cache.TryGetValue(cacheKey, out MockResults? cached) && cached is not null)
            return cached;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(maxTimeMS));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = new MockResults { LoopCount = loopCount, MaxTimeMS = maxTimeMS };

        try
        {
            decimal accumulator = 0;
            for (int i = 0; i < loopCount; i++)
            {
                linkedCts.Token.ThrowIfCancellationRequested();
                accumulator += i * 0.001m;
                await Task.Delay(10, linkedCts.Token);
            }
            sw.Stop();
            results.RunTimeMS = sw.ElapsedMilliseconds;
            results.Message = "Task Complete";
            results.ResultValue = accumulator.ToString("F3");

            cache.Set(cacheKey, results, TimeSpan.FromSeconds(30));
            return results;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            sw.Stop();
            results.RunTimeMS = sw.ElapsedMilliseconds;
            results.Message = "Time Out Occurred";
            results.ResultValue = "408";
            logger.LogWarning("Remote mock timed out after {MaxTimeMS}ms for loopCount={LoopCount}", maxTimeMS, loopCount);
            return results;
        }
    }
}
