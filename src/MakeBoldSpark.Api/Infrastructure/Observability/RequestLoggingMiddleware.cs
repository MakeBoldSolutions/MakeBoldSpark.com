using System.Diagnostics;
using System.Security.Claims;

namespace MakeBoldSpark.Api.Infrastructure.Observability;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
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
