using System.Diagnostics;
using System.Security.Claims;

namespace MakeBoldSpark.Api.Infrastructure.Observability;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    // Bold API request/response bodies (chat messages, provider output) must never reach the logs
    // (spec.md Content Privacy clause, AC6/AC10) — the server is a forwarder, not a store, of
    // workspace content. This middleware never reads or logs any route's request/response body
    // today — the single LogInformation call below only ever includes method/path/status/duration/
    // correlation/user/feature/operation, which are all metadata, never content. IsBoldRoute exists
    // so that guarantee is explicit and testable (BoldContentPrivacyTests) rather than an accident
    // of the current log statement; if a future change ever wants to log request/response bodies
    // for other features, it must branch on IsBoldRoute and skip Bold routes.
    public static bool IsBoldRoute(string path) => path.StartsWith("/api/integrations/bold/v1", StringComparison.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? context.TraceIdentifier;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);

        sw.Stop();

        var path = context.Request.Path.Value ?? "/";
        var segments = path.TrimStart('/').Split('/');
        var featureName = segments.Length > 1 ? segments[1] : segments.FirstOrDefault() ?? "unknown";
        var operationName = context.GetEndpoint()?.DisplayName ?? path;
        var statusCode = context.Response.StatusCode;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var success = statusCode < 400;

        logger.LogInformation(
            "HTTP {Method} {RequestPath} responded {StatusCode} in {DurationMs}ms | CorrelationId={CorrelationId} | UserId={UserId} | Feature={FeatureName} | Operation={OperationName} | Success={Success}",
            context.Request.Method,
            path,
            statusCode,
            sw.ElapsedMilliseconds,
            correlationId,
            userId ?? "anonymous",
            featureName,
            operationName,
            success);
    }
}
