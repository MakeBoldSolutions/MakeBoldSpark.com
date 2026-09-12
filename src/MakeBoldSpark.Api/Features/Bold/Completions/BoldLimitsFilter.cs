using System.Collections.Concurrent;
using MakeBoldSpark.Api.Features.Bold.Auth;
using MakeBoldSpark.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MakeBoldSpark.Api.Features.Bold.Completions;

/// <summary>
/// Per-install request rate limit and monthly cost cap (spec.md Rate limiting &amp; cost caps).
/// Applied only to the completions endpoint — read endpoints (runs, usage, providers) remain
/// available even past the cost cap. The cap is a guardrail, not an invoice limit (accepted risk,
/// critic note 2026-07-21): concurrent in-flight completions may overshoot it by a request or two;
/// no distributed locking is added.
/// </summary>
public class BoldLimitsFilter(MakeBoldSparkDbContext db, IOptions<BoldOptions> options, BoldRateLimiterState rateLimiterState) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var installTokenId = context.HttpContext.User.GetInstallTokenId();
        if (installTokenId is null)
            return BoldErrors.Unauthorized();

        var rateLimitResult = CheckRateLimit(installTokenId.Value);
        if (rateLimitResult is not null)
            return rateLimitResult;

        var costCapResult = await CheckCostCapAsync(installTokenId.Value, context.HttpContext.RequestAborted);
        if (costCapResult is not null)
            return costCapResult;

        return await next(context);
    }

    private IResult? CheckRateLimit(int installTokenId)
    {
        var limit = options.Value.RateLimit.CompletionsPerMinute;
        var now = DateTimeOffset.UtcNow;

        var window = rateLimiterState.Windows.AddOrUpdate(
            installTokenId,
            _ => (now, 1),
            (_, existing) => now - existing.WindowStart >= TimeSpan.FromMinutes(1)
                ? (now, 1)
                : (existing.WindowStart, existing.Count + 1));

        if (window.Count > limit)
        {
            var retryAfter = (int)Math.Ceiling((window.WindowStart.AddMinutes(1) - now).TotalSeconds);
            return BoldErrors.TooManyRequests("rate_limit_exceeded", "Per-install completion rate limit exceeded.", Math.Max(retryAfter, 1), true);
        }

        return null;
    }

    private async Task<IResult?> CheckCostCapAsync(int installTokenId, CancellationToken cancellationToken)
    {
        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        // SQLite's EF provider cannot translate DateTimeOffset comparisons combined with Sum() into
        // SQL, so the (small, per-install, single-month) row set is evaluated client-side.
        var monthToDateCost = (await db.BoldRuns
                .Where(r => r.InstallTokenId == installTokenId)
                .Select(r => new { r.CreatedAt, r.EstimatedCostUsd })
                .ToListAsync(cancellationToken))
            .Where(r => r.CreatedAt >= monthStart)
            .Sum(r => r.EstimatedCostUsd);

        if (monthToDateCost >= options.Value.CostCap.MonthlyLimitUsd)
        {
            return BoldErrors.TooManyRequests(
                "cost_cap_exceeded",
                $"Monthly cost cap of ${options.Value.CostCap.MonthlyLimitUsd:0.00} exceeded for this install.",
                retryAfterSeconds: null,
                retryable: false);
        }

        return null;
    }
}

/// <summary>
/// Holds the in-process fixed-window rate-limit counters as a DI singleton, scoped to this app
/// instance's lifetime — acceptable for the single-instance MVP deployment (spec.md Accepted
/// risks); a multi-instance deployment would need a shared store. Registered as a singleton
/// (rather than a static field) so test hosts that spin up independent
/// <c>WebApplicationFactory</c> instances never share state with each other.
/// </summary>
public class BoldRateLimiterState
{
    public ConcurrentDictionary<int, (DateTimeOffset WindowStart, int Count)> Windows { get; } = new();
}
