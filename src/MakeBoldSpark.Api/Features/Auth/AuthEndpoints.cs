using System.Threading.RateLimiting;
using MakeBoldSpark.Api.Infrastructure.OpenApi;

namespace MakeBoldSpark.Api.Features.Auth;

public static class AuthEndpoints
{
    private static readonly LoginErrorResponse GenericFailure = new();

    // Per-email fixed-window limiter, applied explicitly inside the handler rather than via
    // ASP.NET Core's endpoint-level RateLimiting middleware: the partition key (the submitted
    // email) isn't known until the request body is bound, but endpoint rate-limiter policies
    // partition before binding runs. The IP-keyed policy (login-per-ip, registered in
    // Program.cs) covers the middleware-layer half of the dual-limiter design (research.md).
    private static readonly PartitionedRateLimiter<string> EmailLimiter =
        PartitionedRateLimiter.Create<string, string>(email =>
            RateLimitPartition.GetFixedWindowLimiter(email, _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                QueueLimit = 0,
            }));

    public static RouteGroupBuilder MapAuthApi(this RouteGroupBuilder group)
    {
        // Anonymous by necessity — a sign-in endpoint must be reachable without a token by
        // definition. This is the documented Constitution Waiver for Principle VIII (Clear
        // Authorization Boundaries) in plan.md; a follow-up /devspark.constitution amendment
        // is recommended (tasks.md T054) to add an explicit /api/public/auth/* row to that
        // table. The handler never logs the request body or submitted password — see
        // RequestLoggingMiddleware (logs only method/path/status/duration) and AuthService
        // (logs only the author id on success/failure).
        group.MapPost("/login", async (LoginRequest request, AuthService svc, CancellationToken ct) =>
        {
            using var lease = EmailLimiter.AttemptAcquire(request.Email);
            if (!lease.IsAcquired)
                return Results.Json(GenericFailure, statusCode: StatusCodes.Status401Unauthorized);

            var result = await svc.LoginAsync(request, ct);
            return result is null
                ? Results.Json(GenericFailure, statusCode: StatusCodes.Status401Unauthorized)
                : Results.Ok(result);
        })
        .WithName("Login")
        .WithTags(MakeBoldSparkOpenApiTags.AuthSignIn)
        .WithSummary("Sign in as an administrator")
        .WithDescription("Verifies an Author's email/password and issues a signed JWT. Every rejection reason — unknown email, wrong password, non-admin account, or throttling — returns the identical generic 401 response; never a 429.")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces<LoginErrorResponse>(StatusCodes.Status401Unauthorized)
        .RequireRateLimiting("login-per-ip")
        .AllowAnonymous();

        return group;
    }
}
