namespace MakeBoldSpark.Api.Infrastructure.Cors;

public static class CorsSetup
{
    public const string PolicyName = "MakeBoldSparkPolicy";

    public static IServiceCollection AddMakeBoldSparkCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var origins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                }
                else if (environment.IsDevelopment())
                {
                    // Development fallback only — never fall back to localhost in production
                    policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                          .AllowAnyHeader().AllowAnyMethod();
                }
                // Non-development with empty origins: no cross-origin access (implicit deny)
            });
        });

        return services;
    }
}
