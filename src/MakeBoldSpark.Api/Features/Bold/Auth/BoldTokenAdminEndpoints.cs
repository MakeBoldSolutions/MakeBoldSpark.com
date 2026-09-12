using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Infrastructure.Data.Entities;
using MakeBoldSpark.Api.Infrastructure.OpenApi;
using Microsoft.EntityFrameworkCore;

namespace MakeBoldSpark.Api.Features.Bold.Auth;

public record IssueBoldTokenRequest(string Name);

public record IssueBoldTokenResponse(int Id, string Name, string Token, DateTimeOffset CreatedAt);

/// <summary>
/// Admin-protected install-token lifecycle for the Bold API gateway (spec.md O10). Issuance is an
/// admin operation only — no self-service endpoint (Out of scope) and no local-script issuance
/// (ratified 2026-07-21: production SQLite on Azure App Service is not reachable by ad-hoc scripts).
/// </summary>
public static class BoldTokenAdminEndpoints
{
    public static RouteGroupBuilder MapBoldTokenAdminApi(this RouteGroupBuilder group)
    {
        group.MapPost("/tokens", async (IssueBoldTokenRequest request, MakeBoldSparkDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BoldErrors.BadRequest("invalid_request", "Token 'name' is required.");

            var plaintext = BoldTokenHasher.GenerateToken();
            var entity = new BoldInstallToken
            {
                Name = request.Name.Trim(),
                TokenHash = BoldTokenHasher.Hash(plaintext),
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.BoldInstallTokens.Add(entity);
            await db.SaveChangesAsync(ct);

            // Plaintext returned exactly once — never persisted or logged (spec.md AC2, AC10).
            return Results.Created(
                $"/api/admin/bold/tokens/{entity.Id}",
                new IssueBoldTokenResponse(entity.Id, entity.Name, plaintext, entity.CreatedAt));
        })
        .WithName("IssueBoldInstallToken")
        .WithTags(MakeBoldSparkOpenApiTags.BoldAdmin)
        .WithSummary("Issue a new Bold install token")
        .WithDescription("Admin-only. Returns the plaintext token exactly once; only its SHA-256 hash is persisted.")
        .Produces<IssueBoldTokenResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponseDto>(StatusCodes.Status400BadRequest);

        group.MapDelete("/tokens/{id:int}", async (int id, MakeBoldSparkDbContext db, CancellationToken ct) =>
        {
            var entity = await db.BoldInstallTokens.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (entity is null)
                return BoldErrors.NotFound("not_found", $"Install token {id} was not found.");

            entity.RevokedAt ??= DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("RevokeBoldInstallToken")
        .WithTags(MakeBoldSparkOpenApiTags.BoldAdmin)
        .WithSummary("Revoke a Bold install token")
        .WithDescription("Admin-only. A revoked token immediately fails authentication on every Bold endpoint.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponseDto>(StatusCodes.Status404NotFound);

        return group;
    }
}
