using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Infrastructure.OpenApi;
using MakeBoldSpark.Recipe.Data;

namespace MakeBoldSpark.Api.Features.Health;

public static class AdminHealthEndpoints
{
    public static RouteGroupBuilder MapAdminHealthApi(this RouteGroupBuilder group)
    {
        group.MapGet("/health/deep", async (MakeBoldSparkDbContext db, RecipeDbContext recipeDb, CancellationToken ct) =>
        {
            var checks = new Dictionary<string, string>();
            try
            {
                var canConnect = await db.Database.CanConnectAsync(ct);
                checks["database"] = canConnect ? "ok" : "unavailable";
            }
            catch
            {
                checks["database"] = "error";
            }

            try
            {
                var recipeCanConnect = await recipeDb.Database.CanConnectAsync(ct);
                checks["recipeDatabase"] = recipeCanConnect ? "ok" : "unavailable";
            }
            catch
            {
                checks["recipeDatabase"] = "error";
            }

            var allOk = checks.Values.All(v => v == "ok");
            var response = new DeepHealthResponse(allOk ? "Healthy" : "Degraded", checks);
            return allOk ? Results.Ok(response) : Results.Json(response, statusCode: 503);
        })
        .WithName("GetDeepHealth")
        .WithTags(MakeBoldSparkOpenApiTags.HealthDiagnostics)
        .WithSummary("Deep health probe (admin)")
        .WithDescription("Checks each downstream dependency (database, etc.) and returns 200 when all are healthy or 503 when one or more are degraded. Requires the AdminOnly policy.")
        .Produces<DeepHealthResponse>(200)
        .Produces<DeepHealthResponse>(503);

        return group;
    }
}
