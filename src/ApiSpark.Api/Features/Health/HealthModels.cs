using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ApiSpark.Api.Features.Health;

/// <summary>Standard liveness-check response returned by <c>GET /api/health</c>.</summary>
/// <param name="Status">Current health status, e.g. "Healthy" or "Degraded".</param>
/// <param name="Service">Name of the service, always "ApiSpark".</param>
/// <param name="Version">Informational assembly version of the running build.</param>
public record HealthResponse(
    [property: Required]
    [property: DefaultValue("Healthy")]
    [property: Description("Current health status, such as Healthy or Degraded.")]
    string Status,
    [property: Required]
    [property: DefaultValue("ApiSpark")]
    [property: Description("Name of the running API service.")]
    string Service,
    [property: Required]
    [property: DefaultValue("1.0.0")]
    [property: Description("Informational assembly version of the running build.")]
    string Version);

/// <summary>Deep-health response that includes per-dependency check results.</summary>
/// <param name="Status">Aggregate status: "Healthy" when all checks pass, "Degraded" otherwise.</param>
/// <param name="Checks">Map of dependency name to status string ("ok", "unavailable", or "error").</param>
public record DeepHealthResponse(
    [property: Required]
    [property: DefaultValue("Healthy")]
    [property: Description("Aggregate status: Healthy when all checks pass, Degraded otherwise.")]
    string Status,
    [property: Required]
    [property: Description("Map of dependency name to status string, such as ok, unavailable, or error.")]
    Dictionary<string, string> Checks);
