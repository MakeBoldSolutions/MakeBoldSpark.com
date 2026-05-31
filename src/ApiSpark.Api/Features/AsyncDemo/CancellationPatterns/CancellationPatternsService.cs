namespace ApiSpark.Api.Features.AsyncDemo.CancellationPatterns;

public static class CancellationPatternsService
{
    public static async Task<decimal> RunLoopAsync(int iterations, CancellationToken ct)
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
}
