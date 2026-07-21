using System.Text.Json.Serialization;
using MakeBoldSpark.Api.Features.Bold.Auth;
using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Infrastructure.Data.Entities;
using MakeBoldSpark.Api.Infrastructure.OpenApi;
using Microsoft.EntityFrameworkCore;

namespace MakeBoldSpark.Api.Features.Bold.Runs;

public record UsageDto(
    [property: JsonPropertyName("input_tokens")] int InputTokens,
    [property: JsonPropertyName("output_tokens")] int OutputTokens,
    [property: JsonPropertyName("estimated_cost_usd")] decimal EstimatedCostUsd);

public record RunSummaryDto(
    [property: JsonPropertyName("run_id")] string RunId,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("model_role")] string ModelRole,
    [property: JsonPropertyName("workflow")] string? Workflow,
    [property: JsonPropertyName("workspace_id")] string? WorkspaceId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("usage")] UsageDto Usage,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);

public record RunsPageDto(
    [property: JsonPropertyName("runs")] List<RunSummaryDto> Runs,
    [property: JsonPropertyName("next_cursor")] string? NextCursor);

public record UsageRangeDto(
    [property: JsonPropertyName("since")] DateTimeOffset? Since,
    [property: JsonPropertyName("until")] DateTimeOffset? Until);

public record UsageBreakdownEntryDto(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("usage")] UsageDto Usage);

public record UsageSummaryDto(
    [property: JsonPropertyName("range")] UsageRangeDto Range,
    [property: JsonPropertyName("totals")] UsageDto Totals,
    [property: JsonPropertyName("breakdown")] List<UsageBreakdownEntryDto> Breakdown);

/// <summary>
/// Run history and usage reporting, scoped to the authenticated install (spec.md AC6). `GET /runs`
/// implements opaque keyset pagination — never OFFSET, so pages stay correct as new runs land.
/// </summary>
public static class RunsEndpoints
{
    public static RouteGroupBuilder MapBoldRunsApi(this RouteGroupBuilder group)
    {
        group.MapGet("/runs", GetRunsAsync)
            .WithName("GetBoldRuns")
            .WithTags(MakeBoldSparkOpenApiTags.BoldRuns)
            .WithSummary("List recent runs for the authenticated install")
            .Produces<RunsPageDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponseDto>(StatusCodes.Status401Unauthorized);

        group.MapGet("/runs/{runId}", GetRunByIdAsync)
            .WithName("GetBoldRunById")
            .WithTags(MakeBoldSparkOpenApiTags.BoldRuns)
            .WithSummary("Fetch a single run record")
            .Produces<RunSummaryDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponseDto>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponseDto>(StatusCodes.Status401Unauthorized);

        group.MapGet("/usage", GetUsageAsync)
            .WithName("GetBoldUsage")
            .WithTags(MakeBoldSparkOpenApiTags.BoldUsage)
            .WithSummary("Usage and cost summary for the authenticated install")
            .Produces<UsageSummaryDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponseDto>(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> GetRunsAsync(
        HttpContext httpContext,
        MakeBoldSparkDbContext db,
        string? workspace_id,
        string? workflow,
        DateTimeOffset? since,
        int? limit,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var installTokenId = httpContext.User.GetInstallTokenId();
        if (installTokenId is null) return BoldErrors.Unauthorized();

        var pageSize = Math.Clamp(limit ?? 50, 1, 200);

        var query = db.BoldRuns.AsNoTracking().Where(r => r.InstallTokenId == installTokenId);
        if (!string.IsNullOrWhiteSpace(workspace_id))
            query = query.Where(r => r.WorkspaceId == workspace_id);
        if (!string.IsNullOrWhiteSpace(workflow))
            query = query.Where(r => r.Workflow == workflow);
        if (since is not null)
            query = query.Where(r => r.CreatedAt >= since);

        var decodedCursorId = RunsCursor.TryDecode(cursor);
        if (decodedCursorId is { } cursorId)
        {
            query = query.Where(r => r.Id < cursorId);
        }

        var page = await query
            .OrderByDescending(r => r.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var items = page.Take(pageSize).ToList();
        var nextCursor = hasMore && items.Count > 0
            ? RunsCursor.Encode(items[^1].Id)
            : null;

        return Results.Ok(new RunsPageDto(items.Select(ToSummary).ToList(), nextCursor));
    }

    private static async Task<IResult> GetRunByIdAsync(
        HttpContext httpContext, MakeBoldSparkDbContext db, string runId, CancellationToken cancellationToken)
    {
        var installTokenId = httpContext.User.GetInstallTokenId();
        if (installTokenId is null) return BoldErrors.Unauthorized();

        var run = await db.BoldRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.InstallTokenId == installTokenId && r.RunId == runId, cancellationToken);

        return run is null
            ? BoldErrors.NotFound("not_found", $"Run '{runId}' was not found.")
            : Results.Ok(ToSummary(run));
    }

    private static async Task<IResult> GetUsageAsync(
        HttpContext httpContext,
        MakeBoldSparkDbContext db,
        DateTimeOffset? since,
        DateTimeOffset? until,
        string? group_by,
        CancellationToken cancellationToken)
    {
        var installTokenId = httpContext.User.GetInstallTokenId();
        if (installTokenId is null) return BoldErrors.Unauthorized();

        var query = db.BoldRuns.AsNoTracking().Where(r => r.InstallTokenId == installTokenId);
        if (since is not null) query = query.Where(r => r.CreatedAt >= since);
        if (until is not null) query = query.Where(r => r.CreatedAt <= until);

        var runs = await query.ToListAsync(cancellationToken);

        var totals = new UsageDto(
            runs.Sum(r => r.InputTokens),
            runs.Sum(r => r.OutputTokens),
            runs.Sum(r => r.EstimatedCostUsd));

        var breakdown = group_by switch
        {
            "day" => runs.GroupBy(r => r.CreatedAt.UtcDateTime.Date.ToString("yyyy-MM-dd"))
                .Select(g => new UsageBreakdownEntryDto(g.Key, Aggregate(g))).ToList(),
            "provider" => runs.GroupBy(r => r.Provider)
                .Select(g => new UsageBreakdownEntryDto(g.Key, Aggregate(g))).ToList(),
            "model_role" => runs.GroupBy(r => r.ModelRole)
                .Select(g => new UsageBreakdownEntryDto(g.Key, Aggregate(g))).ToList(),
            "workflow" => runs.Where(r => r.Workflow is not null).GroupBy(r => r.Workflow!)
                .Select(g => new UsageBreakdownEntryDto(g.Key, Aggregate(g))).ToList(),
            _ => [],
        };

        return Results.Ok(new UsageSummaryDto(new UsageRangeDto(since, until), totals, breakdown));
    }

    private static UsageDto Aggregate(IEnumerable<BoldRun> runs)
        => new(runs.Sum(r => r.InputTokens), runs.Sum(r => r.OutputTokens), runs.Sum(r => r.EstimatedCostUsd));

    private static RunSummaryDto ToSummary(BoldRun r) => new(
        r.RunId, r.Provider, r.Model, r.ModelRole, r.Workflow, r.WorkspaceId, r.Status,
        new UsageDto(r.InputTokens, r.OutputTokens, r.EstimatedCostUsd), r.CreatedAt);
}
