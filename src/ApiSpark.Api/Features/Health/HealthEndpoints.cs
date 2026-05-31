using System.Reflection;
using ApiSpark.Api.Infrastructure.OpenApi;

namespace ApiSpark.Api.Features.Health;

public static class HealthEndpoints
{
    public static WebApplication MapHealthApi(this WebApplication app)
    {
        app.MapGet("/api/health", () =>
        {
            var version = Assembly.GetExecutingAssembly()
                              .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                              ?.InformationalVersion ?? "0.1.0";

            return Results.Ok(new HealthResponse("Healthy", "ApiSpark", version));
        })
        .WithName("GetHealth")
        .WithTags(ApiSparkOpenApiTags.HealthDiagnostics)
        .WithSummary("API liveness probe")
        .WithDescription("Returns the current health status and assembly version. Always returns 200 when the process is running — suitable as a Kubernetes liveness or Azure health-check probe.")
        .Produces<HealthResponse>(200)
        .AllowAnonymous();

        return app;
    }
}
