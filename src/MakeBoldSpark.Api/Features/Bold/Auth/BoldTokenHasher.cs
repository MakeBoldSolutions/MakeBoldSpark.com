using System.Security.Cryptography;
using System.Text;

namespace MakeBoldSpark.Api.Features.Bold.Auth;

/// <summary>
/// Generates and hashes Bold install tokens. Tokens are stored hashed (spec.md AC2) — the plaintext
/// value only ever exists in memory during issuance and in the one-time issuance response.
/// </summary>
public static class BoldTokenHasher
{
    private const string TokenPrefix = "bold_";

    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return TokenPrefix + Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static string Hash(string plaintextToken)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintextToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
