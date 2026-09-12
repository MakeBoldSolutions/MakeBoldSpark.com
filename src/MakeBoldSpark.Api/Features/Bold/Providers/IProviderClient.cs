namespace MakeBoldSpark.Api.Features.Bold.Providers;

public record ProviderMessage(string Role, string Content);

public record ProviderCompletionRequest(
    string Model,
    IReadOnlyList<ProviderMessage> Messages,
    int? MaxOutputTokens,
    double? Temperature);

public record ProviderCompletionResult(
    string Content,
    int InputTokens,
    int OutputTokens,
    string? ProviderRequestId);

public record ProviderHealth(bool Reachable, int? LatencyMs, string? Message);

/// <summary>
/// Thin per-provider HTTP client. Error mapping (retryable classification), retry policy, and
/// token-usage extraction stay under this feature's control (spec.md Intent: provider integration
/// style) — clients throw <see cref="BoldProviderException"/> on failure, which
/// <c>CompletionService</c> maps to the contract's 502/422 shapes.
/// </summary>
public interface IProviderClient
{
    /// <summary>"openai" | "anthropic" — matches the contract's Provider enum.</summary>
    string Provider { get; }

    Task<ProviderCompletionResult> CompleteAsync(ProviderCompletionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Bounded reachability probe used by GET /providers. Must respect the same per-attempt timeout
    /// as a completion call so a slow/unreachable provider can never make the endpoint hang
    /// (spec.md AC7).
    /// </summary>
    Task<ProviderHealth> CheckHealthAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Thrown by provider clients on any failure. The <c>retryable</c> flag distinguishes transient
/// failures (timeouts, 5xx, network errors) from hard failures (invalid key, unknown model) per
/// spec.md AC5.
/// </summary>
public class BoldProviderException(string code, string message, bool retryable) : Exception(message)
{
    public string Code { get; } = code;
    public bool Retryable { get; } = retryable;
}
