using System.Text.Json;
using System.Text.Json.Serialization;

namespace MakeBoldSpark.Api.Features.Bold.Completions;

public record MessageDto(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

public record ClientMetadataDto(
    [property: JsonPropertyName("workspace_id")] string? WorkspaceId,
    [property: JsonPropertyName("workflow")] string? Workflow,
    [property: JsonPropertyName("starter_id")] string? StarterId,
    [property: JsonPropertyName("run_label")] string? RunLabel);

public record CompletionRequestDto(
    [property: JsonPropertyName("model_role")] string ModelRole,
    [property: JsonPropertyName("provider")] string? Provider,
    [property: JsonPropertyName("messages")] List<MessageDto> Messages,
    [property: JsonPropertyName("response_format")] string? ResponseFormat,
    [property: JsonPropertyName("response_schema")] JsonElement? ResponseSchema,
    [property: JsonPropertyName("max_output_tokens")] int? MaxOutputTokens,
    [property: JsonPropertyName("temperature")] double? Temperature,
    [property: JsonPropertyName("client_metadata")] ClientMetadataDto? ClientMetadata);

public record UsageDto(
    [property: JsonPropertyName("input_tokens")] int InputTokens,
    [property: JsonPropertyName("output_tokens")] int OutputTokens,
    [property: JsonPropertyName("estimated_cost_usd")] decimal EstimatedCostUsd);

public record CompletionResponseDto(
    [property: JsonPropertyName("run_id")] string RunId,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("model_role")] string ModelRole,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("retries")] int Retries,
    [property: JsonPropertyName("usage")] UsageDto Usage,
    [property: JsonPropertyName("provider_request_id")] string? ProviderRequestId,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);
