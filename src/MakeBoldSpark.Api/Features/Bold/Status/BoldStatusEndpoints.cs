using System.Reflection;
using System.Text.Json.Serialization;
using MakeBoldSpark.Api.Features.Bold.Providers;
using MakeBoldSpark.Api.Infrastructure.OpenApi;
using Microsoft.Extensions.Options;

namespace MakeBoldSpark.Api.Features.Bold.Status;

public record HealthStatusDto(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("time")] DateTimeOffset Time);

public record ProviderStatusDto(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("reachable")] bool Reachable,
    [property: JsonPropertyName("latency_ms")] int? LatencyMs,
    [property: JsonPropertyName("last_checked")] DateTimeOffset LastChecked,
    [property: JsonPropertyName("message")] string? Message);

public record ModelRoleMappingDto(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("model")] string Model);

/// <summary>
/// Read-only Bold status surface: liveness, provider reachability, and current model-role routing
/// (spec.md AC7, AC8). Reflects config directly — changing routing/pricing is a config change, not
/// a code change.
/// </summary>
public static class BoldStatusEndpoints
{
    public static RouteGroupBuilder MapBoldStatusApi(this RouteGroupBuilder group)
    {
        group.MapGet("/health", () =>
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.1.0";
            return Results.Ok(new HealthStatusDto("ok", version, DateTimeOffset.UtcNow));
        })
        .WithName("GetBoldHealth")
        .WithTags(MakeBoldSparkOpenApiTags.BoldHealth)
        .WithSummary("Bold API liveness probe")
        .WithDescription("Anonymous, shallow liveness check — always 200 when the process is running.")
        .Produces<HealthStatusDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        group.MapGet("/providers", async (IEnumerable<IProviderClient> clients, CancellationToken ct) =>
        {
            var statuses = new List<ProviderStatusDto>();
            foreach (var client in clients)
            {
                var health = await client.CheckHealthAsync(ct);
                statuses.Add(new ProviderStatusDto(client.Provider, health.Reachable, health.LatencyMs, DateTimeOffset.UtcNow, health.Message));
            }
            return Results.Ok(new { providers = statuses });
        })
        .WithName("GetBoldProviders")
        .WithTags(MakeBoldSparkOpenApiTags.BoldProviders)
        .WithSummary("List configured providers and their current health")
        .WithDescription("Returns reachability and latency, never credentials. Each check is bounded by the same per-attempt timeout as a completion call.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponseDto>(StatusCodes.Status401Unauthorized);

        group.MapGet("/model-roles", (IOptions<BoldOptions> options) =>
        {
            var roles = options.Value.ModelRoles
                .Select(kv => new ModelRoleMappingDto(kv.Key, kv.Value.Provider, kv.Value.Model));
            return Results.Ok(new { roles });
        })
        .WithName("GetBoldModelRoles")
        .WithTags(MakeBoldSparkOpenApiTags.BoldProviders)
        .WithSummary("Current model-role routing configuration")
        .WithDescription("Read-only view of which provider/model each role currently resolves to. Reflects the server's live BoldOptions configuration.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponseDto>(StatusCodes.Status401Unauthorized);

        return group;
    }
}
