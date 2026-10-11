namespace MakeBoldSpark.Api.Features.ArrowSparkReactions;

/// <summary>Bound from the "ArrowSparkReactions" configuration section (API-110, section 12).</summary>
public sealed class ArrowSparkReactionOptions
{
    public const string SectionName = "ArrowSparkReactions";

    /// <summary>Dedicated directory outside the web root and any repository. Must already exist.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Operator switch (API-81/83): when true the endpoint returns 410 and stores nothing.</summary>
    public bool Disabled { get; set; }

    public int RateLimitPerMinute { get; set; } = 10;

    public int MaxFiles { get; set; } = 10_000;

    public string[] DevelopmentOrigins { get; set; } = [];
}
