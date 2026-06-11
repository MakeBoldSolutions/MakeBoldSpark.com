using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using ApiSpark.Api.Features.AsyncDemo.CancellationPatterns;
using ApiSpark.Api.Features.AsyncDemo.ConcurrencyPatterns;
using ApiSpark.Api.Features.AsyncDemo.RemoteMock;
using ApiSpark.Api.Features.AsyncDemo.Status;
using ApiSpark.Api.Features.AsyncDemo.WeatherPatterns;
using ApiSpark.Api.Features.Health;
using ApiSpark.Api.Features.PublicContent;
using ApiSpark.Api.Features.Recipe;
using ApiSpark.Api.Features.WebSpark;
using ApiSpark.Api.Infrastructure.Auth;
using ApiSpark.Api.Infrastructure.Cors;
using ApiSpark.Api.Infrastructure.Data;
using ApiSpark.Api.Infrastructure.Data.Repositories;
using ApiSpark.Api.Infrastructure.Observability;
using ApiSpark.Api.Infrastructure.OpenApi;
using ApiTestSpark;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using WebSpark.Core.Data;
using WebSpark.Recipe.Data;
using WebSpark.Recipe.Interfaces;
using WebSpark.Recipe.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseShutdownTimeout(TimeSpan.FromSeconds(15));

// Auth (must register scheme + policies — SS-1 fix)
builder.Services.AddApiSparkAuth(builder.Configuration, builder.Environment);

// CORS
builder.Services.AddApiSparkCors(builder.Configuration, builder.Environment);

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
builder.Services.AddDbContext<ApiSparkDbContext>(options =>
    options.UseSqlite(ResolveSqliteConnStr(builder.Configuration.GetConnectionString("DefaultConnection"), contentRoot)));

builder.Services.AddScoped<IContentRepository, ContentRepository>();
builder.Services.AddScoped<ContentService>();

// Recipe data layer
builder.Services.AddDbContext<RecipeDbContext>(options =>
    options.UseSqlite(ResolveSqliteConnStr(builder.Configuration.GetConnectionString("RecipeConnection"), contentRoot)));
builder.Services.AddScoped<IRecipeService, RecipeProvider>();
builder.Services.AddScoped<RecipeService>();

// WebSpark.Core data layer
builder.Services.AddDbContext<WebSparkDbContext>(options =>
    options.UseSqlite(ResolveSqliteConnStr(builder.Configuration.GetConnectionString("WebSparkConnection"), contentRoot))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddScoped<WebSparkService>();

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

// OpenAPI / Scalar (dev only)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "ApiSpark API",
            Version = "v1",
            Description = """
                ApiSpark is a .NET 10 Minimal API showcase with live OpenAPI metadata optimized for API Test Spark.

                | Area | What to try |
                | --- | --- |
                | Public Content | Browse published article summaries, fetch article detail by slug, and list taxonomy tags. |
                | Recipes | Browse approved recipes publicly, then use publisher endpoints to create or update recipes and categories. |
                | WebSpark CMS | Explore domain, blog, author, post, category, menu, keyword, content part, subscriber, newsletter, and mail-setting resources. |
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
            new OpenApiTag { Name = ApiSparkOpenApiTags.HealthDiagnostics, Description = "Liveness and deep-health probes for the API and its dependencies." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.PublicContentArticles, Description = "Read-only access to published article summaries and detail pages." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.PublicContentTags, Description = "Taxonomy tags used to classify public content." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.RecipesCatalog, Description = "Public recipe browsing and detail lookup." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.RecipesCategories, Description = "Recipe category browsing and category management." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.RecipesPublishing, Description = "Publisher-only recipe creation, update, and deletion." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkPublicDomains, Description = "Anonymous read access to CMS site and domain records." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkPublicBlogs, Description = "Anonymous read access to CMS blog containers." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkPublicAuthors, Description = "Anonymous read access to public author profiles. Password hashes are never returned." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkPublicPosts, Description = "Anonymous read access to CMS posts and published content entries." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkPublicCategories, Description = "Anonymous read access to CMS categories." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkPublicMenus, Description = "Anonymous read access to CMS navigation menus." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkPublicKeywords, Description = "Anonymous read access to CMS keyword and SEO tag records." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkPublicContentParts, Description = "Anonymous read access to reusable CMS content fragments and page parts." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkAdminDomains, Description = "Admin-only CMS domain creation, update, and deletion." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkAdminBlogs, Description = "Admin-only CMS blog creation, update, and deletion." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkAdminAuthors, Description = "Admin-only CMS author creation, update, and deletion. Responses never return password hashes." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkAdminPosts, Description = "Admin-only CMS post creation, update, and deletion." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkAdminCategories, Description = "Admin-only CMS category creation, update, and deletion." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkAdminMenus, Description = "Admin-only CMS menu creation, update, and deletion." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkAdminKeywords, Description = "Admin-only CMS keyword creation, update, and deletion." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkAdminContentParts, Description = "Admin-only CMS content-part creation, update, and deletion." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkMessagingSubscribers, Description = "Admin-only CMS newsletter subscriber records." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkMessagingNewsletters, Description = "Admin-only CMS newsletter campaign records." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.WebSparkMessagingMailSettings, Description = "Admin-only CMS mail sender configuration. SMTP passwords are accepted in requests but never returned." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.AsyncWeatherPatterns, Description = "Demonstrates async best-practices using live weather data: slow baseline, timeout, retry with Polly, and parallel fan-out." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.AsyncCancellationPatterns, Description = "Contrasts operations with and without CancellationToken support, including linked timeout tokens." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.AsyncConcurrencyPatterns, Description = "Compares sequential execution, unbounded Task.WhenAll, and SemaphoreSlim-throttled concurrency." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.AsyncResilienceTimeouts, Description = "Simulates a slow downstream service to exercise retry, timeout, and circuit-breaker patterns." },
            new OpenApiTag { Name = ApiSparkOpenApiTags.AsyncMonitoringHealth, Description = "Application status, build metadata, and configuration diagnostics for the async demo feature set." },
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

// OpenAPI and Scalar served in all environments for this public demo API.
// Restrict via network-level controls or the Environments setting below if needed.
app.MapOpenApi();                // /openapi/v1.json
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();     // /scalar/v1
}

// Route groups
var publicApi = app.MapGroup("/api/public");
publicApi.MapPublicContentApi();
publicApi.MapPublicRecipeApi();
publicApi.MapGroup("/webspark").MapPublicWebSparkApi();

var adminApi = app.MapGroup("/api/admin")
    .RequireAuthorization("AdminOnly");
adminApi.MapAdminHealthApi();
adminApi.MapGroup("/webspark").MapAdminWebSparkApi();

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
