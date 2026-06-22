using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MakeBoldSpark.Core.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MakeBoldSpark.Api.Features.Auth;

public class AuthService(MakeBoldSparkCoreDbContext db, IConfiguration configuration, ILogger<AuthService> logger)
{
    private const int MaxPasswordLength = 256;
    private static readonly PasswordHasher<Author> Hasher = new();
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (request.Password.Length > MaxPasswordLength)
        {
            logger.LogInformation("Login rejected: submitted password exceeded the maximum length");
            return null;
        }

        var author = await db.Authors.SingleOrDefaultAsync(a => a.Email == request.Email, ct);
        if (author is null || !author.IsAdmin)
        {
            logger.LogInformation("Login rejected: no matching administrator account");
            return null;
        }

        if (Hasher.VerifyHashedPassword(author, author.Password, request.Password) == PasswordVerificationResult.Failed)
        {
            logger.LogInformation("Login rejected: password did not verify for author {AuthorId}", author.Id);
            return null;
        }

        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);

        var token = new JwtSecurityToken(
            audience: configuration["Jwt:Audience"],
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, author.Id.ToString()),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Name, author.DisplayName),
            ],
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));

        logger.LogInformation("Login succeeded for author {AuthorId}", author.Id);

        return new LoginResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            DisplayName = author.DisplayName,
        };
    }
}
