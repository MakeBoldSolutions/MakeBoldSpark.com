using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MakeBoldSpark.Core.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MakeBoldSpark.Api.Features.Auth;

public class AuthService(
    MakeBoldSparkCoreDbContext db,
    IConfiguration configuration,
    ILogger<AuthService> logger,
    IPasswordHasher<Author> passwordHasher)
{
    private const int MaxPasswordLength = 256;
    private static readonly PasswordHasher<Author> TimingSafeHasher = new();
    private static readonly Author TimingSafeAuthor = CreateTimingSafeAuthor();
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (request.Password.Length > MaxPasswordLength)
        {
            logger.LogInformation("Login rejected: submitted password exceeded the maximum length");
            return null;
        }

        var author = await db.Authors.SingleOrDefaultAsync(a => a.Email == request.Email, ct);
        // Verify exactly one PBKDF2 hash for every normal rejection path. Without this dummy
        // target, unknown/non-admin emails return before hashing while a wrong administrator
        // password does not, exposing account eligibility through response timing.
        var verificationTarget = author is { IsAdmin: true } ? author : TimingSafeAuthor;
        var passwordMatches = passwordHasher.VerifyHashedPassword(
            verificationTarget,
            verificationTarget.Password,
            request.Password) != PasswordVerificationResult.Failed;

        if (author is null || !author.IsAdmin || !passwordMatches)
        {
            logger.LogInformation("Login rejected: credentials did not verify");
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

    private static Author CreateTimingSafeAuthor()
    {
        var author = new Author
        {
            Email = "timing-safe-placeholder@example.invalid",
            Password = string.Empty,
            DisplayName = "Timing-safe placeholder",
            IsAdmin = false,
        };
        author.Password = TimingSafeHasher.HashPassword(author, "not-a-login-credential");
        return author;
    }
}
