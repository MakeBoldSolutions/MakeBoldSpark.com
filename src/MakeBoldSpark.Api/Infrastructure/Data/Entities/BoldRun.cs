namespace MakeBoldSpark.Api.Infrastructure.Data.Entities;

/// <summary>
/// Metadata-only record of a single Bold API completion (or embeddings) call. Per spec.md's Content
/// Privacy clause, this row never contains message bodies or provider output text — only accounting
/// and routing metadata. `WorkspaceId`/`Workflow` are optional (null when the client omits
/// ClientMetadata) and such rows are excluded from workspace/workflow-scoped filters.
/// </summary>
public class BoldRun
{
    public int Id { get; set; }
    public string RunId { get; set; } = Guid.NewGuid().ToString("n");
    public int InstallTokenId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ModelRole { get; set; } = string.Empty;
    public string? Workflow { get; set; }
    public string? WorkspaceId { get; set; }
    public string? StarterId { get; set; }
    public string? RunLabel { get; set; }

    /// <summary>succeeded | failed | retried</summary>
    public string Status { get; set; } = string.Empty;
    public int Retries { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public string? ProviderRequestId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
