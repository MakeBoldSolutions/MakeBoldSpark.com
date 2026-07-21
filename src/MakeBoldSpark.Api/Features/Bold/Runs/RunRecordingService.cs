using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Infrastructure.Data.Entities;
using Microsoft.Extensions.Options;

namespace MakeBoldSpark.Api.Features.Bold.Runs;

public record RunRecordInput(
    int InstallTokenId,
    string Provider,
    string Model,
    string ModelRole,
    string? Workflow,
    string? WorkspaceId,
    string? StarterId,
    string? RunLabel,
    string Status,
    int Retries,
    int InputTokens,
    int OutputTokens,
    string? ErrorCode,
    string? ErrorMessage,
    long DurationMs,
    string? ProviderRequestId);

/// <summary>
/// Persists metadata-only run records and computes estimated cost from the static per-model price
/// table (spec.md Content Privacy clause: never message bodies or provider output text).
/// </summary>
public class RunRecordingService(MakeBoldSparkDbContext db, IOptions<BoldOptions> options)
{
    public decimal EstimateCostUsd(string model, int inputTokens, int outputTokens)
    {
        var pricing = options.Value.ResolvePricing(model);
        if (pricing is null) return 0m;

        var inputCost = inputTokens / 1_000_000m * pricing.InputPerMillionUsd;
        var outputCost = outputTokens / 1_000_000m * pricing.OutputPerMillionUsd;
        return Math.Round(inputCost + outputCost, 6);
    }

    public async Task<BoldRun> RecordAsync(RunRecordInput input, CancellationToken cancellationToken)
    {
        var entity = new BoldRun
        {
            RunId = Guid.NewGuid().ToString("n"),
            InstallTokenId = input.InstallTokenId,
            Provider = input.Provider,
            Model = input.Model,
            ModelRole = input.ModelRole,
            Workflow = input.Workflow,
            WorkspaceId = input.WorkspaceId,
            StarterId = input.StarterId,
            RunLabel = input.RunLabel,
            Status = input.Status,
            Retries = input.Retries,
            InputTokens = input.InputTokens,
            OutputTokens = input.OutputTokens,
            EstimatedCostUsd = EstimateCostUsd(input.Model, input.InputTokens, input.OutputTokens),
            ErrorCode = input.ErrorCode,
            ErrorMessage = input.ErrorMessage,
            DurationMs = input.DurationMs,
            ProviderRequestId = input.ProviderRequestId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.BoldRuns.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
