namespace MakeBoldSpark.Api.Features.Bold.Completions;

/// <summary>Exponential transient-error backoff: 250 ms initially, bounded to five seconds.</summary>
public class CompletionRetryDelay
{
    public virtual Task WaitAsync(int retry, CancellationToken cancellationToken)
        => Task.Delay(GetDuration(retry), cancellationToken);

    public static TimeSpan GetDuration(int retry)
        => TimeSpan.FromMilliseconds(Math.Min(5000, 250 * Math.Pow(2, Math.Clamp(retry - 1, 0, 5))));
}
