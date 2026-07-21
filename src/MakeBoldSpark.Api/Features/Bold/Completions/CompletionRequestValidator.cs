using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MakeBoldSpark.Api.Features.Bold.Completions;

public record ValidationOutcome(bool IsValid, string? ErrorCode, string? ErrorMessage)
{
    public static ValidationOutcome Ok { get; } = new(true, null, null);
    public static ValidationOutcome Fail(string code, string message) => new(false, code, message);
}

/// <summary>
/// Config-driven request bounds validated before any provider call (spec.md Request bounds,
/// critic blocker fix 2026-07-21): max messages, max request size, max_output_tokens ceiling, and
/// max response_schema size/nesting depth — keeps schema validation from being a DoS vector.
/// </summary>
public class CompletionRequestValidator(IOptions<BoldOptions> options)
{
    private static readonly string[] ValidRoles = ["router", "planner", "reviewer", "embedding"];
    private static readonly string[] ValidMessageRoles = ["system", "user", "assistant"];

    public ValidationOutcome Validate(CompletionRequestDto request, int requestBodyBytes)
    {
        var bounds = options.Value.RequestBounds;

        if (string.IsNullOrWhiteSpace(request.ModelRole) || !ValidRoles.Contains(request.ModelRole))
            return ValidationOutcome.Fail("invalid_request", $"'model_role' must be one of: {string.Join(", ", ValidRoles)}.");

        if (request.Provider is not null && request.Provider is not ("openai" or "anthropic"))
            return ValidationOutcome.Fail("invalid_request", "'provider' must be 'openai' or 'anthropic' when specified.");

        if (request.Messages is null || request.Messages.Count == 0)
            return ValidationOutcome.Fail("invalid_request", "'messages' must contain at least one entry.");

        if (request.Messages.Count > bounds.MaxMessages)
            return ValidationOutcome.Fail("request_too_large", $"'messages' exceeds the maximum of {bounds.MaxMessages} entries.");

        foreach (var message in request.Messages)
        {
            if (string.IsNullOrWhiteSpace(message.Role) || !ValidMessageRoles.Contains(message.Role))
                return ValidationOutcome.Fail("invalid_request", $"Message role must be one of: {string.Join(", ", ValidMessageRoles)}.");
            if (message.Content is null)
                return ValidationOutcome.Fail("invalid_request", "Every message must have 'content'.");
        }

        if (requestBodyBytes > bounds.MaxRequestBytes)
            return ValidationOutcome.Fail("request_too_large", $"Request body exceeds the maximum of {bounds.MaxRequestBytes} bytes.");

        if (request.ResponseFormat is "json" && request.ResponseSchema is not null)
        {
            var schemaJson = JsonSerializer.Serialize(request.ResponseSchema);
            if (System.Text.Encoding.UTF8.GetByteCount(schemaJson) > bounds.MaxSchemaBytes)
                return ValidationOutcome.Fail("schema_too_large", $"'response_schema' exceeds the maximum of {bounds.MaxSchemaBytes} bytes.");

            var depth = ComputeDepth(request.ResponseSchema.Value);
            if (depth > bounds.MaxSchemaDepth)
                return ValidationOutcome.Fail("schema_too_large", $"'response_schema' exceeds the maximum nesting depth of {bounds.MaxSchemaDepth}.");
        }

        if (request.MaxOutputTokens is int requested && requested <= 0)
            return ValidationOutcome.Fail("invalid_request", "'max_output_tokens' must be a positive integer when specified.");

        return ValidationOutcome.Ok;
    }

    /// <summary>Effective max_output_tokens: client value capped by the server-side ceiling; the
    /// ceiling itself when the client omits or exceeds it (spec.md Request bounds).</summary>
    public int ResolveEffectiveMaxOutputTokens(int? requested)
    {
        var ceiling = options.Value.RequestBounds.MaxOutputTokensCeiling;
        if (requested is null || requested <= 0 || requested > ceiling)
            return ceiling;
        return requested.Value;
    }

    private static int ComputeDepth(JsonElement element, int current = 1)
    {
        if (current > 64) return current; // guard against pathological input while measuring
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().Any()
                ? element.EnumerateObject().Max(p => ComputeDepth(p.Value, current + 1))
                : current,
            JsonValueKind.Array => element.EnumerateArray().Any()
                ? element.EnumerateArray().Max(e => ComputeDepth(e, current + 1))
                : current,
            _ => current,
        };
    }
}
