using System.Threading.RateLimiting;
using MakeBoldSpark.Api.Features.Bold.Completions;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace MakeBoldSpark.Api.Features.ArrowSparkReactions;

/// <summary>
/// Anonymous, write-only intake for ArrowSpark showcase reactions. One route, no read path (API-01, API-65).
/// </summary>
public static class ArrowSparkReactionEndpoints
{
    public const string Route = "/api/public/arrowspark/reactions";
    public const string CorsPolicy = "ArrowSparkReactionsPublic";
    public const string RateLimitPolicy = "arrowspark-reactions";
    private const string ProductionOrigin = "https://arrow.makeboldspark.com";

    public static IServiceCollection AddArrowSparkReactions(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ArrowSparkReactionOptions.SectionName);
        services.AddOptions<ArrowSparkReactionOptions>().Bind(section);
        services.AddSingleton<ArrowSparkReactionStore>();
        services.AddHostedService<ArrowSparkTempCleanupService>();

        var settings = section.Get<ArrowSparkReactionOptions>() ?? new ArrowSparkReactionOptions();
        foreach (var origin in settings.DevelopmentOrigins)
        {
            if (origin.Contains('*') || !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || (uri.Scheme != "http" && uri.Scheme != "https")
                || origin != uri.GetLeftPart(UriPartial.Authority) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidOperationException("ArrowSparkReactions:DevelopmentOrigins must contain explicit HTTP(S) origins without paths or wildcards.");
        }

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins([ProductionOrigin, .. settings.DevelopmentOrigins])
            .WithMethods("POST").WithHeaders("Content-Type")
            .SetPreflightMaxAge(TimeSpan.FromSeconds(600))));

        // Partition key is the connection IP and lives only in limiter memory (API-50, API-51).
        services.AddRateLimiter(options => options.AddPolicy(RateLimitPolicy, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = Math.Max(1, httpContext.RequestServices
                        .GetRequiredService<IOptionsMonitor<ArrowSparkReactionOptions>>().CurrentValue.RateLimitPerMinute),
                    QueueLimit = 0,
                })));
        return services;
    }

    public static bool IsReactionRoute(PathString path) => path.StartsWithSegments(Route, StringComparison.OrdinalIgnoreCase);

    public static void MapArrowSparkReactions(this WebApplication app)
    {
        app.MapPost(Route, HandleAsync)
            .AllowAnonymous().RequireCors(CorsPolicy).RequireRateLimiting(RateLimitPolicy)
            .AddEndpointFilter<ArrowSparkResponseFilter>()
            .WithTags("ArrowSpark Reactions").WithName("PostArrowSparkReaction")
            .WithSummary("Accepts one anonymous ArrowSpark game or story reaction; insert-only, no read path.")
            .Produces(202).ProducesProblem(400).Produces(410).Produces(413).Produces(415).Produces(429).ProducesProblem(500).Produces(503);
    }

    private static async Task<IResult> HandleAsync(
        HttpContext context,
        ArrowSparkReactionStore store,
        IOptionsMonitor<ArrowSparkReactionOptions> options,
        CancellationToken ct)
    {
        if (options.CurrentValue.Disabled) return Results.StatusCode(StatusCodes.Status410Gone);

        if (!IsJson(context.Request.ContentType)) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

        // Enforce the size limit before any parsing (API-20).
        var limit = ArrowSparkReactionValidator.MaxBodyBytes;
        if (context.Request.ContentLength > limit) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        var body = await BoundedRequestBody.ReadAsync(context.Request.Body, limit, ct);
        if (body is null) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        var result = ArrowSparkReactionValidator.Validate(body);
        if (!result.IsValid)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid reaction.",
                extensions: new Dictionary<string, object?> { ["property"] = result.Property });

        var outcome = await store.SaveAsync(result.ReactionType!, result.CanonicalPayload!, ct);
        // 202 only after the file has been renamed into place (API-31).
        return outcome == ArrowSparkStoreOutcome.Stored
            ? Results.StatusCode(StatusCodes.Status202Accepted)
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    private static bool IsJson(string? contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var media)
        && string.Equals(media.MediaType.Value, "application/json", StringComparison.OrdinalIgnoreCase)
        && (!media.Charset.HasValue || string.Equals(media.Charset.Value, "utf-8", StringComparison.OrdinalIgnoreCase));
}

/// <summary>No-store and nosniff on every response; on failure logs only exception type and trace id (API-93).</summary>
public sealed class ArrowSparkResponseFilter(ILogger<ArrowSparkResponseFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var headers = context.HttpContext.Response.Headers;
        headers.CacheControl = "no-store";
        headers.XContentTypeOptions = "nosniff";
        try { return await next(context); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("ArrowSpark reaction request failed ({ExceptionType}); TraceId={TraceId}",
                ex.GetType().Name, context.HttpContext.TraceIdentifier);
            return Results.Problem(statusCode: 500, title: "Unable to complete the request.");
        }
    }
}
