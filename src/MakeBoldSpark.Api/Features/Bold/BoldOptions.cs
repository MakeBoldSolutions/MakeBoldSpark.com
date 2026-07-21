namespace MakeBoldSpark.Api.Features.Bold;

/// <summary>
/// Server-side configuration for the Bold API gateway feature. Bound from the "Bold" configuration
/// section (app settings locally, Azure App Service settings in deployed environments — backbone X,
/// zero secrets in source control). Routing, pricing, and limits are all config-driven so they can
/// change without a code release (spec.md Intent: "Gateway, not passthrough").
/// </summary>
public class BoldOptions
{
    public const string SectionName = "Bold";

    /// <summary>model_role (router/planner/reviewer/embedding) -> provider/model mapping.</summary>
    public Dictionary<string, ModelRoleMapping> ModelRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["router"] = new ModelRoleMapping { Provider = "openai", Model = "gpt-5-mini" },
        ["planner"] = new ModelRoleMapping { Provider = "anthropic", Model = "claude-sonnet-4-5" },
        ["reviewer"] = new ModelRoleMapping { Provider = "openai", Model = "gpt-5.1" },
    };

    /// <summary>Static per-model USD price table (input/output rate per million tokens).</summary>
    public Dictionary<string, ModelPricing> Pricing { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gpt-5-mini"] = new ModelPricing { InputPerMillionUsd = 0.25m, OutputPerMillionUsd = 2.00m },
        ["gpt-5.1"] = new ModelPricing { InputPerMillionUsd = 2.50m, OutputPerMillionUsd = 10.00m },
        ["claude-sonnet-4-5"] = new ModelPricing { InputPerMillionUsd = 3.00m, OutputPerMillionUsd = 15.00m },
    };

    public OpenAiProviderOptions OpenAi { get; set; } = new();

    public AnthropicProviderOptions Anthropic { get; set; } = new();

    public RateLimitOptions RateLimit { get; set; } = new();

    public CostCapOptions CostCap { get; set; } = new();

    public RequestBoundsOptions RequestBounds { get; set; } = new();

    public ProviderCallOptions ProviderCall { get; set; } = new();

    public ModelRoleMapping? ResolveRole(string role)
        => ModelRoles.TryGetValue(role, out var mapping) ? mapping : null;

    public ModelPricing? ResolvePricing(string model)
        => Pricing.TryGetValue(model, out var pricing) ? pricing : null;
}

public class ModelRoleMapping
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}

public class ModelPricing
{
    public decimal InputPerMillionUsd { get; set; }
    public decimal OutputPerMillionUsd { get; set; }
}

public class OpenAiProviderOptions
{
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public string? ApiKey { get; set; }
}

public class AnthropicProviderOptions
{
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public string? ApiKey { get; set; }
    public string ApiVersion { get; set; } = "2023-06-01";
}

/// <summary>Per-install request rate limit (spec.md: default 30 completions/minute).</summary>
public class RateLimitOptions
{
    public int CompletionsPerMinute { get; set; } = 30;
}

/// <summary>Per-install monthly cost ceiling (spec.md: default $50 USD/install).</summary>
public class CostCapOptions
{
    public decimal MonthlyLimitUsd { get; set; } = 50.00m;
}

/// <summary>Config-driven caps validated before any provider call (spec.md Request bounds).</summary>
public class RequestBoundsOptions
{
    public int MaxMessages { get; set; } = 50;
    public int MaxRequestBytes { get; set; } = 400 * 1024;
    public int MaxOutputTokensCeiling { get; set; } = 16_384;
    public int MaxSchemaBytes { get; set; } = 64 * 1024;
    public int MaxSchemaDepth { get; set; } = 10;
}

/// <summary>Provider call resilience (spec.md: default 120s per-attempt timeout, 2 retries).</summary>
public class ProviderCallOptions
{
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxRetries { get; set; } = 2;
}
