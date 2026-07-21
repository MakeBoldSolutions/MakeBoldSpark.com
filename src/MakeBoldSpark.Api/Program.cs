using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using MakeBoldSpark.Api.Features.AsyncDemo.CancellationPatterns;
using MakeBoldSpark.Api.Features.AsyncDemo.ConcurrencyPatterns;
using MakeBoldSpark.Api.Features.AsyncDemo.RemoteMock;
using MakeBoldSpark.Api.Features.AsyncDemo.Status;
using MakeBoldSpark.Api.Features.AsyncDemo.WeatherPatterns;
using MakeBoldSpark.Api.Features.Auth;
using MakeBoldSpark.Api.Features.Bold;
using MakeBoldSpark.Api.Features.Bold.Auth;
using MakeBoldSpark.Api.Features.Bold.Completions;
using MakeBoldSpark.Api.Features.Bold.Embeddings;
using MakeBoldSpark.Api.Features.Bold.Providers;
using MakeBoldSpark.Api.Features.Bold.Runs;
using MakeBoldSpark.Api.Features.Bold.Status;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;
using MakeBoldSpark.Cms;
using MakeBoldSpark.Recipe.Client;
using MakeBoldSpark.Core.Data;
using MakeBoldSpark.Core.Infrastructure.Logging;
using MakeBoldSpark.Recipe.Data;
using MakeBoldSpark.Recipe.Interfaces;
using MakeBoldSpark.Recipe.Providers;

// Repeatable administrator-provisioning procedure (tasks.md T014, gate finding critic-007):
//   dotnet run --project src/MakeBoldSpark.Api -- bootstrap-admin <email> <password> [displayName]
// Hashes the password with the same PasswordHasher<Author> the login endpoint verifies
// against, then inserts or updates that Author row with isAdmin = true, and exits — does not
// start the host. Kept isolated from normal startup so it never runs unintentionally.
if (args.Length > 0 && args[0] == "bootstrap-admin")
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: dotnet run --project src/MakeBoldSpark.Api -- bootstrap-admin <email> <password> [displayName]");
        Environment.Exit(1);
        return;
    }

    var bootstrapConfig = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build();

    var optionsBuilder = new DbContextOptionsBuilder<MakeBoldSparkCoreDbContext>()
        .UseSqlite(bootstrapConfig.GetConnectionString("MakeBoldSparkConnection"));
    using var bootstrapDb = new MakeBoldSparkCoreDbContext(optionsBuilder.Options);

    var email = args[1];
    var password = args[2];
    var displayName = args.Length > 3 ? args[3] : email;

    var existing = await bootstrapDb.Authors.SingleOrDefaultAsync(a => a.Email == email);
    var author = existing ?? new Author { Email = email, Password = string.Empty, DisplayName = displayName, IsAdmin = true };
    author.Password = new PasswordHasher<Author>().HashPassword(author, password);
    author.IsAdmin = true;
    author.DisplayName = displayName;

    if (existing is null)
    {
        // Older deployed databases retain required DateCreated/DateUpdated columns from the
        // pre-BaseEntity schema. They are no longer mapped by EF, so an ordinary insert leaves
        // DateCreated null and makes the documented bootstrap command unusable. Detect that
        // legacy shape and populate both audit-column generations until a dedicated migration
        // removes the obsolete columns.
        var connection = bootstrapDb.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            await using var legacyColumnCheck = connection.CreateCommand();
            legacyColumnCheck.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Authors') WHERE name IN ('DateCreated', 'DateUpdated');";
            var hasLegacyAuditColumns = Convert.ToInt32(await legacyColumnCheck.ExecuteScalarAsync()) == 2;

            if (hasLegacyAuditColumns)
            {
                var now = DateTime.UtcNow;
                author.CreatedDate = now;
                author.UpdatedDate = now;

                await using var insert = connection.CreateCommand();
                insert.CommandText = """
                    INSERT INTO "Authors" ("Email", "Password", "DisplayName", "IsAdmin", "CreatedDate", "DateCreated", "DateUpdated", "UpdatedDate")
                    VALUES ($email, $password, $displayName, $isAdmin, $createdDate, $dateCreated, $dateUpdated, $updatedDate);
                    """;
                void AddParameter(string name, object value)
                {
                    var parameter = insert.CreateParameter();
                    parameter.ParameterName = name;
                    parameter.Value = value;
                    insert.Parameters.Add(parameter);
                }

                AddParameter("$email", author.Email);
                AddParameter("$password", author.Password);
                AddParameter("$displayName", author.DisplayName);
                AddParameter("$isAdmin", author.IsAdmin);
                AddParameter("$createdDate", author.CreatedDate);
                AddParameter("$dateCreated", now);
                AddParameter("$dateUpdated", now);
                AddParameter("$updatedDate", author.UpdatedDate);
                await insert.ExecuteNonQueryAsync();
            }
            else
            {
                bootstrapDb.Authors.Add(author);
                await bootstrapDb.SaveChangesAsync();
            }
        }
        finally
        {
            await connection.CloseAsync();
        }

        // The legacy insert runs outside EF tracking. Reload so the success message reports
        // the generated identifier and future changes use the normal tracked entity.
        author = await bootstrapDb.Authors.SingleAsync(a => a.Email == email);
    }
    else
    {
        await bootstrapDb.SaveChangesAsync();
    }

    Console.WriteLine($"Administrator credential set for '{email}' (Author Id {author.Id}).");
    return;
}

var builder = WebApplication.CreateBuilder(args);

LoggingUtility.ConfigureLogging(builder, "MakeBoldSpark.Api");

builder.WebHost.UseShutdownTimeout(TimeSpan.FromSeconds(15));

// Auth (must register scheme + policies — SS-1 fix)
builder.Services.AddMakeBoldSparkAuth(builder.Configuration, builder.Environment);

// CORS
builder.Services.AddMakeBoldSparkCors(builder.Configuration, builder.Environment);

// Login throttle (FR-014) — IP/global-keyed half of the dual-layer design (research.md);
// the email-keyed half is applied explicitly inside AuthEndpoints.MapAuthApi. Either layer
// being exceeded returns the same generic 401 the login endpoint uses for any other
// rejection reason — never a distinguishable 429 (closes SC-006).
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, token) =>
    {
        if (context.HttpContext.Request.Path.StartsWithSegments("/api/public/auth"))
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.HttpContext.Response.WriteAsJsonAsync(new LoginErrorResponse(), cancellationToken: token);
            return;
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many publisher recipe operations. Retry shortly." },
            cancellationToken: token);
    };

    options.AddPolicy("login-per-ip", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 20,
            QueueLimit = 0,
        }));

    options.AddPolicy("publisher-recipe-mutation", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous-publisher",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 120,
            QueueLimit = 0,
        }));
});

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

// Bold API gateway configuration (no secrets in repo — provider keys come from Azure App Service
// settings / user-secrets at the "Bold:OpenAi:ApiKey" / "Bold:Anthropic:ApiKey" paths).
builder.Services.AddOptions<BoldOptions>()
    .Bind(builder.Configuration.GetSection(BoldOptions.SectionName));

var boldOptionsForStartup = builder.Configuration.GetSection(BoldOptions.SectionName).Get<BoldOptions>() ?? new BoldOptions();
var boldProviderTimeout = TimeSpan.FromSeconds(boldOptionsForStartup.ProviderCall.TimeoutSeconds);

builder.Services.AddHttpClient(OpenAiProviderClient.HttpClientName, client =>
{
    client.BaseAddress = new Uri(boldOptionsForStartup.OpenAi.BaseUrl);
    client.Timeout = boldProviderTimeout;
});
builder.Services.AddHttpClient(AnthropicProviderClient.HttpClientName, client =>
{
    client.BaseAddress = new Uri(boldOptionsForStartup.Anthropic.BaseUrl);
    client.Timeout = boldProviderTimeout;
});
builder.Services.AddScoped<IProviderClient, OpenAiProviderClient>();
builder.Services.AddScoped<IProviderClient, AnthropicProviderClient>();

builder.Services.AddScoped<CompletionRequestValidator>();
builder.Services.AddScoped<CompletionService>();
builder.Services.AddScoped<RunRecordingService>();
builder.Services.AddSingleton<BoldRateLimiterState>();
builder.Services.AddScoped<BoldLimitsFilter>();

// MakeBoldSpark.Core data layer
builder.Services.AddDbContext<MakeBoldSparkCoreDbContext>(options =>
    options.UseSqlite(ResolveSqliteConnStr(builder.Configuration.GetConnectionString("MakeBoldSparkConnection"), contentRoot))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddScoped<MakeBoldSparkService>();
builder.Services.AddSingleton<IPasswordHasher<Author>, PasswordHasher<Author>>();
builder.Services.AddScoped<AuthService>();

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
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.AuthSignIn, Description = "Signed JWT sign-in for CMS administrators under the dedicated anonymous /api/public/auth authorization category." },
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
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.BoldAdmin, Description = "Admin-only issuance and revocation of Bold API install tokens." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.BoldHealth, Description = "Anonymous, shallow liveness check for the Bold API gateway." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.BoldProviders, Description = "Provider reachability and model-role routing visibility for Bold Desktop/CLI." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.BoldCompletions, Description = "Model-role completion gateway behind Bold's plan/build/ship workflows." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.BoldEmbeddings, Description = "Post-MVP embeddings endpoint; currently returns a stable not_implemented error." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.BoldRuns, Description = "Run history for the authenticated Bold install." },
            new OpenApiTag { Name = MakeBoldSparkOpenApiTags.BoldUsage, Description = "Token/cost usage reporting for the authenticated Bold install." },
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
app.UseRateLimiter();

// OpenAPI served in all environments for this public demo API.
// Restrict via network-level controls or the Environments setting below if needed.
app.MapOpenApi();                // /openapi/v1.json

// Route groups
var publicApi = app.MapGroup("/api/public");
publicApi.MapPublicContentApi();
publicApi.MapPublicRecipeApi();
publicApi.MapGroup("/makeboldspark").MapPublicMakeBoldSparkApi();
// Anonymous credential-verification route. Principle VIII reserves /api/public/auth/* for
// credential verification and signed-token issuance only; it does not permit CMS-data access.
publicApi.MapGroup("/auth").MapAuthApi();

var adminApi = app.MapGroup("/api/admin")
    .RequireAuthorization("AdminOnly");
adminApi.MapAdminHealthApi();
adminApi.MapGroup("/makeboldspark").MapAdminMakeBoldSparkApi();
// Bold install-token issuance (spec.md O10) — admin operation only, no self-service endpoint.
adminApi.MapGroup("/bold").MapBoldTokenAdminApi();

var publishApi = app.MapGroup("/api/publish").RequireAuthorization("Publisher");
publishApi.MapPublisherRecipeMaintenanceApi();

// Bold API gateway (spec.md alignment decision 2): contract paths are /v1/* on
// api.makeboldspark.com; internally mounted under /api/integrations/bold/v1/* (service-token
// category, backbone VIII). Every route requires the BoldInstallToken policy except /health,
// which is additionally anonymous per the contract.
var boldApi = app.MapGroup("/api/integrations/bold/v1")
    .RequireAuthorization(BoldInstallTokenDefaults.PolicyName);
boldApi.MapBoldStatusApi();
boldApi.MapBoldCompletionsApi();
boldApi.MapBoldEmbeddingsApi();
boldApi.MapBoldRunsApi();

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

app.MapMakeBoldSparkCms();
app.MapMakeBoldSparkRecipeClient();

// Warn if weather key is missing
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
if (string.IsNullOrWhiteSpace(weatherKey) || weatherKey == "KEYMISSING")
    startupLogger.LogWarning("OpenWeatherMapApiKey is missing or set to placeholder. Weather endpoints will return errors.");

// Database initialization
await DatabaseSetup.InitializeAsync(app, app.Lifetime.ApplicationStopping);

await app.RunAsync();

// Required for WebApplicationFactory<Program> in test project (SS-2 fix)
public partial class Program { }
