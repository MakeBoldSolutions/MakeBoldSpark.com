using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using MakeBoldSpark.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MakeBoldSpark.Api.Features.Bold.Auth;

/// <summary>
/// Well-known scheme name for the Bold install-token bearer scheme. Deliberately distinct from
/// JwtBearer (the default admin scheme) so the two are never interchangeable (spec.md alignment
/// decision 3, ADR 0014): an install token can never satisfy an admin policy (JwtBearer validation
/// rejects it as an invalid JWT) and admin JWTs can never satisfy a Bold client policy (this handler
/// does a hashed-token lookup that a JWT will never match).
/// </summary>
public static class BoldInstallTokenDefaults
{
    public const string AuthenticationScheme = "BoldInstallToken";
    public const string InstallTokenIdClaimType = "bold:install_token_id";
    public const string PolicyName = "BoldInstallToken";
}

public class InstallTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    MakeBoldSparkDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
            return AuthenticateResult.Fail("Missing Authorization header.");

        var authHeader = authHeaderValues.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Authorization header is not a Bearer token.");

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.Fail("Bearer token is empty.");

        var tokenHash = BoldTokenHasher.Hash(token);
        var record = await db.BoldInstallTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, Context.RequestAborted);

        if (record is null || record.IsRevoked)
            return AuthenticateResult.Fail("Invalid or revoked install token.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, record.Id.ToString()),
            new(BoldInstallTokenDefaults.InstallTokenIdClaimType, record.Id.ToString()),
            new("bold:install_name", record.Name),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        var body = new ErrorResponseDto(new ErrorDetailDto("unauthorized", "Missing or invalid install token.", false));
        await Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json";
        var body = new ErrorResponseDto(new ErrorDetailDto("forbidden", "Install token is not permitted to access this resource.", false));
        await Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}

/// <summary>Resolves the authenticated install token's numeric id from the current principal.</summary>
public static class BoldPrincipalExtensions
{
    public static int? GetInstallTokenId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(BoldInstallTokenDefaults.InstallTokenIdClaimType);
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
