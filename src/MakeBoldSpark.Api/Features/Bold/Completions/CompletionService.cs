using System.Diagnostics;
using System.Text.Json;
using Json.Schema;
using MakeBoldSpark.Api.Features.Bold.Providers;
using Microsoft.Extensions.Options;

namespace MakeBoldSpark.Api.Features.Bold.Completions;

public record CompletionOutcome(
    bool Success,
    string Provider,
    string Model,
    string ModelRole,
    string Status, // succeeded | retried | failed
    int Retries,
    int InputTokens,
    int OutputTokens,
    string? Content,
    string? ProviderRequestId,
    long DurationMs,
    string? ErrorCode,
    string? ErrorMessage,
    bool Retryable,
    int HttpStatus);

/// <summary>
/// Role routing + completion orchestration (spec.md Intent: gateway, not passthrough). Resolves
/// model_role to a provider/model, executes the call, validates JSON output against a
/// client-supplied schema with bounded retry, and maps provider failures to the contract's error
/// shapes.
/// </summary>
public class CompletionService(
    IEnumerable<IProviderClient> providerClients,
    IOptions<BoldOptions> options,
    CompletionRequestValidator validator,
    ILogger<CompletionService> logger)
{
    public async Task<CompletionOutcome> ExecuteAsync(CompletionRequestDto request, CancellationToken cancellationToken)
    {
        var mapping = options.Value.ResolveRole(request.ModelRole);
        if (mapping is null)
        {
            return new CompletionOutcome(false, request.Provider ?? "unknown", "unknown", request.ModelRole, "failed", 0, 0, 0, null, null, 0,
                "unsupported_role", $"model_role '{request.ModelRole}' has no configured provider/model mapping.", false, StatusCodes.Status400BadRequest);
        }

        var providerName = request.Provider ?? mapping.Provider;
        var client = providerClients.FirstOrDefault(c => c.Provider == providerName);
        if (client is null)
        {
            return new CompletionOutcome(false, providerName, mapping.Model, request.ModelRole, "failed", 0, 0, 0, null, null, 0,
                "unsupported_provider", $"Provider '{providerName}' is not configured.", false, StatusCodes.Status400BadRequest);
        }

        var effectiveMaxTokens = validator.ResolveEffectiveMaxOutputTokens(request.MaxOutputTokens);
        var providerMessages = request.Messages.Select(m => new ProviderMessage(m.Role, m.Content)).ToList();
        var maxRetries = options.Value.ProviderCall.MaxRetries;
        var wantsJson = request.ResponseFormat == "json" && request.ResponseSchema is not null;

        var sw = Stopwatch.StartNew();
        var retries = 0;

        while (true)
        {
            ProviderCompletionResult providerResult;
            try
            {
                providerResult = await client.CompleteAsync(
                    new ProviderCompletionRequest(mapping.Model, providerMessages, effectiveMaxTokens, request.Temperature),
                    cancellationToken);
            }
            catch (BoldProviderException ex)
            {
                if (ex.Retryable && retries < maxRetries)
                {
                    retries++;
                    logger.LogWarning("Bold completion retry {Retries}/{MaxRetries} after provider error {Code}", retries, maxRetries, ex.Code);
                    continue;
                }

                sw.Stop();
                return new CompletionOutcome(false, providerName, mapping.Model, request.ModelRole, "failed", retries, 0, 0, null, null, sw.ElapsedMilliseconds,
                    ex.Code, ex.Message, ex.Retryable, StatusCodes.Status502BadGateway);
            }

            if (wantsJson)
            {
                var schemaError = ValidateAgainstSchema(providerResult.Content, request.ResponseSchema!.Value);
                if (schemaError is not null)
                {
                    if (retries < maxRetries)
                    {
                        retries++;
                        logger.LogWarning("Bold completion retry {Retries}/{MaxRetries} after schema validation failure", retries, maxRetries);
                        continue;
                    }

                    sw.Stop();
                    return new CompletionOutcome(false, providerName, mapping.Model, request.ModelRole,
                        "failed", retries, providerResult.InputTokens, providerResult.OutputTokens, null,
                        providerResult.ProviderRequestId, sw.ElapsedMilliseconds,
                        "schema_validation_failed", $"Provider output failed schema validation after {retries} retries: {schemaError}", false,
                        StatusCodes.Status422UnprocessableEntity);
                }
            }

            sw.Stop();
            var status = retries > 0 ? "retried" : "succeeded";
            return new CompletionOutcome(true, providerName, mapping.Model, request.ModelRole, status, retries,
                providerResult.InputTokens, providerResult.OutputTokens, providerResult.Content, providerResult.ProviderRequestId,
                sw.ElapsedMilliseconds, null, null, false, StatusCodes.Status200OK);
        }
    }

    private static string? ValidateAgainstSchema(string content, JsonElement schemaElement)
    {
        JsonDocument instanceDoc;
        try
        {
            instanceDoc = JsonDocument.Parse(content);
        }
        catch (JsonException)
        {
            return "Provider output is not valid JSON.";
        }

        using (instanceDoc)
        {
            JsonSchema schema;
            try
            {
                schema = JsonSchema.FromText(schemaElement.GetRawText());
            }
            catch (Exception ex)
            {
                return $"response_schema could not be parsed: {ex.Message}";
            }

            var result = schema.Evaluate(instanceDoc.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (result.IsValid) return null;

            var firstError = (result.Details ?? [])
                .SelectMany(d => d.Errors ?? new Dictionary<string, string>())
                .Select(e => e.Value)
                .FirstOrDefault();
            return firstError ?? "Provider output did not satisfy response_schema.";
        }
    }
}
