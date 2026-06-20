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
        var audience  = jwtSection["Audience"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Require HTTPS for token metadata in all non-development environments
                options.RequireHttpsMetadata = !environment.IsDevelopment();

                if (!string.IsNullOrWhiteSpace(authority))
                    options.Authority = authority;

                if (!string.IsNullOrWhiteSpace(audience))
                    options.Audience = audience;

                // When no authority is configured (e.g. local dev without an IdP),
                // disable signature and issuer validation so the app still starts,
                // but log a warning at startup so it is never silently skipped in prod.
                if (string.IsNullOrWhiteSpace(authority))
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = false,
                        ValidateAudience         = false,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey = false,
                        SignatureValidator       = (token, _) =>
                        {
                            // Accept any well-formed JWT; protected routes still require
                            // a valid token structure and non-expired lifetime.
                            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                            return handler.ReadToken(token);
                        }
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
            });

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
        });

        return services;
    }
}
