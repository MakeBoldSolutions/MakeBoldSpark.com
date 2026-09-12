namespace MakeBoldSpark.Api.Infrastructure.Data.Entities;

/// <summary>
/// A per-install bearer token for the Bold API gateway (spec.md O10). Tokens are stored hashed —
/// the plaintext value is returned exactly once, at issuance, and never persisted or logged.
/// </summary>
public class BoldInstallToken
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;
}
