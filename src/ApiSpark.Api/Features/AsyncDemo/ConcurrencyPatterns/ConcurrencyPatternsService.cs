namespace ApiSpark.Api.Features.AsyncDemo.ConcurrencyPatterns;

public static class ConcurrencyPatternsService
{
    public static async Task<(long ElapsedMs, decimal Result)> RunOperationAsync(
        int operationId, int iterations, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        decimal result = 0;
        for (int i = 0; i < iterations; i++)
        {
            ct.ThrowIfCancellationRequested();
            result += i * 0.5m;
            await Task.Delay(1, ct);
        }
        sw.Stop();
        return (sw.ElapsedMilliseconds, result);
    }
}
