using System.Text;
using MakeBoldSpark.Api.Features.Bold.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MakeBoldSpark.Api.Infrastructure.Auth;

public static class AuthorizationSetup
{
    public static IServiceCollection AddMakeBoldSparkAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var authority = jwtSection["Authority"];
        var audience = jwtSection["Audience"];
        var signingKey = jwtSection["SigningKey"];

        if (string.IsNullOrWhiteSpace(authority) && string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "JWT authentication is not configured: neither 'Jwt:Authority' nor 'Jwt:SigningKey' is set. " +
                "Set 'Jwt:SigningKey' via 'dotnet user-secrets' (local development) or an App Service application " +
                "setting (deployed environments) before starting the API — see quickstart.md.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Require HTTPS for token metadata in all non-development environments
                options.RequireHttpsMetadata = !environment.IsDevelopment();

                if (!string.IsNullOrWhiteSpace(authority))
                    options.Authority = authority;

                if (!string.IsNullOrWhiteSpace(audience))
                    options.Audience = audience;

                if (!string.IsNullOrWhiteSpace(signingKey))
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    };
                }

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = 401;
                        ctx.Response.Headers.WWWAuthenticate = "Bearer";
                        return Task.CompletedTask;
                    }
                };
            })
            // Bold install-token scheme (ADR 0014): a dedicated, non-JWT bearer scheme fully
            // isolated from JwtBearer above. Only requested by the "BoldInstallToken" policy below —
            // AdminOnly/Publisher/ServiceOrAdmin never add it, so a valid install token is simply a
            // malformed JWT to those policies (401), and a valid admin JWT never matches an
            // install-token hash lookup (401) — see BoldAuthTests for the isolation proof.
            .AddScheme<AuthenticationSchemeOptions, InstallTokenAuthenticationHandler>(
                BoldInstallTokenDefaults.AuthenticationScheme, null);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });

            options.AddPolicy("Publisher", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin", "Publisher");
            });

            options.AddPolicy("ServiceOrAdmin", policy =>
            {
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.HasClaim("scope", "makeboldspark.publish"));
            });

            options.AddPolicy(BoldInstallTokenDefaults.PolicyName, policy =>
            {
                policy.AddAuthenticationSchemes(BoldInstallTokenDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            });
        });

        return services;
    }
}
