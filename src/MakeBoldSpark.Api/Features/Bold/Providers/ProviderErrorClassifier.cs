using System.Net;

namespace MakeBoldSpark.Api.Features.Bold.Providers;

/// <summary>
/// Maps a provider HTTP response status to the contract's retryable classification (spec.md AC5).
/// Timeouts and 5xx are transient (retryable); 401/403/404/model-not-found are hard failures.
/// </summary>
public static class ProviderErrorClassifier
{
    public static BoldProviderException Classify(string providerName, HttpStatusCode statusCode, string? providerMessage)
    {
        var message = string.IsNullOrWhiteSpace(providerMessage)
            ? $"{providerName} request failed with status {(int)statusCode}."
            : providerMessage;

        return statusCode switch
        {
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout =>
                new BoldProviderException("provider_timeout", $"{providerName} request timed out: {message}", true),
            HttpStatusCode.TooManyRequests =>
                new BoldProviderException("provider_rate_limited", $"{providerName} rate limited the request: {message}", true),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new BoldProviderException("provider_invalid_key", $"{providerName} rejected the configured API key: {message}", false),
            HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity or HttpStatusCode.BadRequest =>
                new BoldProviderException("model_unavailable", $"{providerName} could not service the request: {message}", false),
            _ when (int)statusCode >= 500 =>
                new BoldProviderException("provider_unavailable", $"{providerName} is unavailable: {message}", true),
            _ =>
                new BoldProviderException("provider_unavailable", $"{providerName} request failed: {message}", false),
        };
    }
}
