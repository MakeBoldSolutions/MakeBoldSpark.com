using System.Text;
using System.Text.Json;
using MakeBoldSpark.Api.Features.Bold.Auth;
using MakeBoldSpark.Api.Features.Bold.Runs;
using MakeBoldSpark.Api.Infrastructure.OpenApi;

namespace MakeBoldSpark.Api.Features.Bold.Completions;

/// <summary>
/// Core model-execution endpoint (spec.md AC3, AC4, AC5): validator → per-install rate/cost limits
/// → role-routing orchestrator → metadata-only run recording.
/// </summary>
public static class CompletionsEndpoints
{
    public static RouteGroupBuilder MapBoldCompletionsApi(this RouteGroupBuilder group)
    {
        group.MapPost("/completions", HandleAsync)
            .WithName("PostBoldCompletion")
            .WithTags(MakeBoldSparkOpenApiTags.BoldCompletions)
            .WithSummary("Execute a model-role completion")
            .WithDescription("Resolves model_role to a provider/model, executes the call, validates structured output, and retries on invalid output or a transient provider failure.")
            .Produces<CompletionResponseDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponseDto>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponseDto>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponseDto>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponseDto>(StatusCodes.Status429TooManyRequests)
            .Produces<ErrorResponseDto>(StatusCodes.Status502BadGateway)
            .AddEndpointFilter<BoldLimitsFilter>();

        return group;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        CompletionRequestValidator validator,
        CompletionService completionService,
        RunRecordingService runRecording,
        CancellationToken cancellationToken)
    {
        var installTokenId = httpContext.User.GetInstallTokenId();
        if (installTokenId is null)
            return BoldErrors.Unauthorized();

        httpContext.Request.EnableBuffering();
        using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        httpContext.Request.Body.Position = 0;

        CompletionRequestDto? request;
        try
        {
            request = JsonSerializer.Deserialize<CompletionRequestDto>(rawBody);
        }
        catch (JsonException)
        {
            return BoldErrors.BadRequest("invalid_request", "Request body is not valid JSON.");
        }

        if (request is null)
            return BoldErrors.BadRequest("invalid_request", "Request body must not be empty.");

        var requestBytes = Encoding.UTF8.GetByteCount(rawBody);
        var validation = validator.Validate(request, requestBytes);
        if (!validation.IsValid)
            return BoldErrors.BadRequest(validation.ErrorCode!, validation.ErrorMessage!);

        var outcome = await completionService.ExecuteAsync(request, cancellationToken);

        var clientMetadata = request.ClientMetadata;
        var run = await runRecording.RecordAsync(new RunRecordInput(
            installTokenId.Value,
            outcome.Provider,
            outcome.Model,
            outcome.ModelRole,
            clientMetadata?.Workflow,
            clientMetadata?.WorkspaceId,
            clientMetadata?.StarterId,
            clientMetadata?.RunLabel,
            outcome.Status,
            outcome.Retries,
            outcome.InputTokens,
            outcome.OutputTokens,
            outcome.ErrorCode,
            outcome.ErrorMessage,
            outcome.DurationMs,
            outcome.ProviderRequestId), cancellationToken);

        if (!outcome.Success)
        {
            return outcome.HttpStatus switch
            {
                StatusCodes.Status400BadRequest => BoldErrors.BadRequest(outcome.ErrorCode!, outcome.ErrorMessage!),
                StatusCodes.Status422UnprocessableEntity => BoldErrors.UnprocessableEntity(outcome.ErrorCode!, outcome.ErrorMessage!),
                _ => BoldErrors.BadGateway(outcome.ErrorCode!, outcome.ErrorMessage!, outcome.Retryable),
            };
        }

        var response = new CompletionResponseDto(
            RunId: run.RunId,
            Provider: outcome.Provider,
            Model: outcome.Model,
            ModelRole: outcome.ModelRole,
            Content: outcome.Content ?? string.Empty,
            Retries: outcome.Retries,
            Usage: new UsageDto(outcome.InputTokens, outcome.OutputTokens, run.EstimatedCostUsd),
            ProviderRequestId: outcome.ProviderRequestId,
            CreatedAt: run.CreatedAt);

        return Results.Ok(response);
    }
}
