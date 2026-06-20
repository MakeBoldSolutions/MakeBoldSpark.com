using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using MakeBoldSpark.Api.Features.AsyncDemo.CancellationPatterns;
using MakeBoldSpark.Api.Features.AsyncDemo.ConcurrencyPatterns;
using MakeBoldSpark.Api.Features.AsyncDemo.RemoteMock;
using MakeBoldSpark.Api.Features.AsyncDemo.Status;
using MakeBoldSpark.Api.Features.AsyncDemo.WeatherPatterns;
using MakeBoldSpark.Api.Features.Health;
using MakeBoldSpark.Api.Features.PublicContent;
using MakeBoldSpark.Api.Features.Recipe;
using MakeBoldSpark.Api.Features.MakeBoldSpark;
using MakeBoldSpark.Api.Infrastructure.Auth;
using MakeBoldSpark.Api.Infrastructure.Cors;
using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Infrastructure.Data.Repositories;
using MakeBoldSpark.Api.Infrastructure.Observability;
using MakeBoldSpark.Api.Infrastructure.OpenApi;
using ApiTestSpark;
using Microsoft.EntityFrameworkCore;
using MakeBoldSpark.Core.Data;
using MakeBoldSpark.Recipe.Data;
using MakeBoldSpark.Recipe.Interfaces;
using MakeBoldSpark.Recipe.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseShutdownTimeout(TimeSpan.FromSeconds(15));

// Auth (must register scheme + policies — SS-1 fix)
builder.Services.AddMakeBoldSparkAuth(builder.Configuration, builder.Environment);

// CORS
builder.Services.AddMakeBoldSparkCors(builder.Configuration, builder.Environment);

// Resolve a SQLite "Data Source=<path>" against ContentRootPath so relative
// paths work correctly under IIS (which sets a different working directory).
var contentRoot = builder.Environment.ContentRootPath;
static string ResolveSqliteConnStr(string? connStr, string root)
{
    if (string.IsNullOrEmpty(connStr)) return string.Empty;
    var parts = connStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
    return string.Join(';', parts.Select(part =>
    {
        var kv = part.Split('=', 2);
        if (kv.Length == 2 && kv[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
        {
            var src = kv[1].Trim();
            if (!Path.IsPathRooted(src))
                src = Path.GetFullPath(Path.Combine(root, src));
            return $"Data Source={src}";
        }
        return part;
    }));
}

// Data layer
builder.Services.AddDbContext<MakeBoldSparkDbContext>(options =>
    options.UseSqlite(ResolveSqliteConnStr(builder.Configuration.GetConnectionString("DefaultConnection"), contentRoot)));

builder.Services.AddScoped<IContentRepository, ContentRepository>();
builder.Services.AddScoped<ContentService>();

// Recipe data layer
builder.Services.AddDbContext<RecipeDbContext>(options =>
    options.UseSqlite(ResolveSqliteConnStr(builder.Configuration.GetConnectionString("RecipeConnection"), contentRoot)));
builder.Services.AddScoped<IRecipeService, RecipeProvider>();
builder.Services.AddScoped<RecipeService>();

// MakeBoldSpark.Core data layer
builder.Services.AddDbContext<MakeBoldSparkCoreDbContext>(options =>
    options.UseSqlite(ResolveSqliteConnStr(builder.Configuration.GetConnectionString("MakeBoldSparkConnection"), contentRoot))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddScoped<MakeBoldSparkService>();

// Named HttpClient for OpenWeatherMap
builder.Services.AddHttpClient("weather", client =>
{
    client.BaseAddress = new Uri("http://api.openweathermap.org");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// AsyncDemo services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAsyncDemoWeatherService, OpenWeatherMapWeatherService>();
builder.Services.AddScoped<RemoteMockService>();

var weatherKey = builder.Configuration["OpenWeatherMapApiKey"];

// OpenAPI document generation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "MakeBoldSpark API",
            Version = "v1",
            Description = """
                MakeBoldSpark is a .NET 10 Minimal API showcase with live OpenAPI metadata optimized for API Test Spark.

                | Area | What to try |
                | --- | --- |
                | Public Content | Browse published article summaries, fetch article detail by slug, and list taxonomy tags. |
                | Recipes | Browse approved recipes publicly, then use publisher endpoints to create or update recipes and categories. |
                | MakeBoldSpark CMS | Explore domain, blog, author, post, category, menu, keyword, content part, subscriber, newsletter, and mail-setting resources. |
                | Async Demo | Compare timeout, retry, cancellation, concurrency, remote-mock, and status endpoints. |

                Suggested workflow:

                1. Start with `GET /api/health` to confirm the host is alive.
                2. Run `GET /api/public/content/articles`, then use one returned slug with `GET /api/public/content/articles/{slug}`.
                3. Run `GET /api/public/recipes` and `GET /api/public/recipes/categories` to inspect seeded recipe data.
                4. Exercise async scenarios under `/api/async-demo/weather`, `/api/async-demo/cancellation`, and `/api/async-demo/concurrency`.
                5. Add a Bearer token before calling admin or publisher endpoints.
                """,
            Contact = new OpenApiContact
            {
                Name = "Mark Hazleton",
                Url = new Uri("https://markhazleton.com")
            },
            License = new OpenApiLicense
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        };

        document.Tags = new HashSet<OpenApiTag>
        {
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.HealthDiagnostics, Description = "Liveness and deep-health probes for the API and its dependencies." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.PublicContentArticles, Description = "Read-only access to published article summaries and detail pages." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.PublicContentTags, Description = "Taxonomy tags used to classify public content." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.RecipesCatalog, Description = "Public recipe browsing and detail lookup." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.RecipesCategories, Description = "Recipe category browsing and category management." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.RecipesPublishing, Description = "Publisher-only recipe creation, update, and deletion." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkPublicDomains, Description = "Anonymous read access to CMS site and domain records." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkPublicBlogs, Description = "Anonymous read access to CMS blog containers." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkPublicAuthors, Description = "Anonymous read access to public author profiles. Password hashes are never returned." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkPublicPosts, Description = "Anonymous read access to CMS posts and published content entries." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkPublicCategories, Description = "Anonymous read access to CMS categories." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkPublicMenus, Description = "Anonymous read access to CMS navigation menus." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkPublicKeywords, Description = "Anonymous read access to CMS keyword and SEO tag records." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkPublicContentParts, Description = "Anonymous read access to reusable CMS content fragments and page parts." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkAdminDomains, Description = "Admin-only CMS domain creation, update, and deletion." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkAdminBlogs, Description = "Admin-only CMS blog creation, update, and deletion." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkAdminAuthors, Description = "Admin-only CMS author creation, update, and deletion. Responses never return password hashes." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkAdminPosts, Description = "Admin-only CMS post creation, update, and deletion." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkAdminCategories, Description = "Admin-only CMS category creation, update, and deletion." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkAdminMenus, Description = "Admin-only CMS menu creation, update, and deletion." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkAdminKeywords, Description = "Admin-only CMS keyword creation, update, and deletion." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkAdminContentParts, Description = "Admin-only CMS content-part creation, update, and deletion." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingSubscribers, Description = "Admin-only CMS newsletter subscriber records." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingNewsletters, Description = "Admin-only CMS newsletter campaign records." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingMailSettings, Description = "Admin-only CMS mail sender configuration. SMTP passwords are accepted in requests but never returned." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.AsyncWeatherPatterns, Description = "Demonstrates async best-practices using live weather data: slow baseline, timeout, retry with Polly, and parallel fan-out." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.AsyncCancellationPatterns, Description = "Contrasts operations with and without CancellationToken support, including linked timeout tokens." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.AsyncConcurrencyPatterns, Description = "Compares sequential execution, unbounded Task.WhenAll, and SemaphoreSlim-throttled concurrency." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.AsyncResilienceTimeouts, Description = "Simulates a slow downstream service to exercise retry, timeout, and circuit-breaker patterns." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.AsyncMonitoringHealth, Description = "Application status, build metadata, and configuration diagnostics for the async demo feature set." },
        };
        return Task.CompletedTask;
    });

    options.AddDocumentTransformer((document, context, ct) =>
    {
        var components = document.Components ?? new OpenApiComponents();
        document.Components = components;
        // OpenApiComponents does not initialize SecuritySchemes in its ctor — must create it explicitly
        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            Description  = "JWT Bearer token. Pass it as: Authorization: Bearer <token>"
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors(CorsSetup.PolicyName);
app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();    // serves wwwroot/**
app.UseAuthentication();
app.UseAuthorization();

// OpenAPI served in all environments for this public demo API.
// Restrict via network-level controls or the Environments setting below if needed.
app.MapOpenApi();                // /openapi/v1.json

// Route groups
var publicApi = app.MapGroup("/api/public");
publicApi.MapPublicContentApi();
publicApi.MapPublicRecipeApi();
publicApi.MapGroup("/makeboldspark").MapPublicMakeBoldSparkApi();

var adminApi = app.MapGroup("/api/admin")
    .RequireAuthorization("AdminOnly");
adminApi.MapAdminHealthApi();
adminApi.MapGroup("/makeboldspark").MapAdminMakeBoldSparkApi();

var publishApi = app.MapGroup("/api/publish").RequireAuthorization("Publisher");
publishApi.MapPublishRecipeApi();

app.MapGroup("/api/integrations").RequireAuthorization("ServiceOrAdmin");

// AsyncDemo route group
var asyncDemoApi = app.MapGroup("/api/async-demo");
asyncDemoApi.MapGroup("/weather").MapWeatherPatternsApi();
asyncDemoApi.MapGroup("/cancellation").MapCancellationPatternsApi();
asyncDemoApi.MapGroup("/concurrency").MapConcurrencyPatternsApi();
asyncDemoApi.MapGroup("/remote").MapRemoteMockApi();
asyncDemoApi.MapGroup("/status").MapAsyncStatusApi();

// Health
app.MapHealthApi();

app.MapApiTestSpark(options =>
{
    options.OpenApiUrl  = "/openapi/v1.json";
    options.AuthScheme  = "Bearer";
    options.Environments = ["Development", "Production", "Release", "Test"];
    options.EnableDemoIntegrations = false;
    options.RemoteApiProfiles.Add(new RemoteApiProfile
    {
        Id = "ui-makeboldspark",
        Name = "UI Make Bold Spark",
        Description = "UI Sample Spark API hosted at ui.makeboldspark.com.",
        RemoteBaseUrl = "https://ui.makeboldspark.com",
        RemoteOpenApiUrl = "https://ui.makeboldspark.com/swagger/v1/swagger.json",
    });
});



// Warn if weather key is missing
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
if (string.IsNullOrWhiteSpace(weatherKey) || weatherKey == "KEYMISSING")
    startupLogger.LogWarning("OpenWeatherMapApiKey is missing or set to placeholder. Weather endpoints will return errors.");

// Database initialization
await DatabaseSetup.InitializeAsync(app, app.Lifetime.ApplicationStopping);

await app.RunAsync();

// Required for WebApplicationFactory<Program> in test project (SS-2 fix)
public partial class Program { }
