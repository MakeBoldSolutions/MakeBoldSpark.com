using System.Security.Claims;
using System.Text.Encodings.Web;
using MakeBoldSpark.Api.Features.Bold.Auth;
using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MakeBoldSpark.Core.Data;
using MakeBoldSpark.Recipe.Data;

namespace MakeBoldSpark.Api.Tests.Infrastructure;

public class MakeBoldSparkWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;
    private SqliteConnection? _recipeConnection;
    private SqliteConnection? _makeBoldSparkConnection;

    private readonly string _dbName        = $"TestDb_{Guid.NewGuid():N}";
    private readonly string _recipeDbName  = $"RecipeTestDb_{Guid.NewGuid():N}";
    private readonly string _makeBoldSparkDbName = $"MakeBoldSparkTestDb_{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection($"Data Source={_dbName};Mode=Memory;Cache=Shared;");
        await _connection.OpenAsync();

        _recipeConnection = new SqliteConnection($"Data Source={_recipeDbName};Mode=Memory;Cache=Shared;");
        await _recipeConnection.OpenAsync();

        _makeBoldSparkConnection = new SqliteConnection($"Data Source={_makeBoldSparkDbName};Mode=Memory;Cache=Shared;");
        await _makeBoldSparkConnection.OpenAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        if (_recipeConnection is not null)
            await _recipeConnection.DisposeAsync();
        if (_makeBoldSparkConnection is not null)
            await _makeBoldSparkConnection.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // DatabaseSetup detects Mode=Memory and uses EnsureCreated() instead of MigrateAsync()
        // for Recipe and MakeBoldSpark contexts, bypassing the legacy migration history bugs.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"]  = $"Data Source={_dbName};Mode=Memory;Cache=Shared;",
                ["ConnectionStrings:RecipeConnection"]   = $"Data Source={_recipeDbName};Mode=Memory;Cache=Shared;",
                ["ConnectionStrings:MakeBoldSparkConnection"] = $"Data Source={_makeBoldSparkDbName};Mode=Memory;Cache=Shared;",
                ["Database:ApplyMigrationsOnStartup"]    = "true",
                ["Database:SeedOnStartup"]               = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace MakeBoldSparkDbContext
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MakeBoldSparkDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddDbContext<MakeBoldSparkDbContext>(options =>
                options.UseSqlite($"Data Source={_dbName};Mode=Memory;Cache=Shared;"));

            // Replace RecipeDbContext with its own in-memory database
            var recipeDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RecipeDbContext>));
            if (recipeDescriptor is not null) services.Remove(recipeDescriptor);
            services.AddDbContext<RecipeDbContext>(options =>
                options.UseSqlite($"Data Source={_recipeDbName};Mode=Memory;Cache=Shared;"));

            // Replace MakeBoldSparkCoreDbContext with its own in-memory database
            var makeBoldSparkDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MakeBoldSparkCoreDbContext>));
            if (makeBoldSparkDescriptor is not null) services.Remove(makeBoldSparkDescriptor);
            services.AddDbContext<MakeBoldSparkCoreDbContext>(options =>
                options.UseSqlite($"Data Source={_makeBoldSparkDbName};Mode=Memory;Cache=Shared;")
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

            // Replace JWT Bearer with test auth handler
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", null);

            // Replace the real provider HttpClients with fakes so Bold tests never hit the network
            // (spec.md AC9: no live provider keys/calls in CI). Individual tests can point
            // OpenAiHandler/AnthropicHandler at a scenario-specific response before issuing a
            // request; both default to a generic success so unrelated tests (e.g. /providers) work
            // without per-test setup.
            // Named HttpClient configuration actions accumulate; registering
            // ConfigurePrimaryHttpMessageHandler again for the same name replaces the handler
            // factory used at build time, so no explicit removal of the Program.cs registration is
            // needed here.
            services.AddHttpClient(MakeBoldSpark.Api.Features.Bold.Providers.OpenAiProviderClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new DelegatingTestHandler(() => OpenAiHandler));

            services.AddHttpClient(MakeBoldSpark.Api.Features.Bold.Providers.AnthropicProviderClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new DelegatingTestHandler(() => AnthropicHandler));
        });
    }

    /// <summary>Per-test override for the fake OpenAI HttpClient response. Null = generic 200 OK.</summary>
    public Func<HttpRequestMessage, HttpResponseMessage>? OpenAiHandler { get; set; }

    /// <summary>Per-test override for the fake Anthropic HttpClient response. Null = generic 200 OK.</summary>
    public Func<HttpRequestMessage, HttpResponseMessage>? AnthropicHandler { get; set; }

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "Admin");
        return client;
    }

    public HttpClient CreatePublisherClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "Publisher");
        return client;
    }

    /// <summary>
    /// Seeds a Bold install token directly into the test database (bypassing the admin issuance
    /// endpoint, since tests need the plaintext value) and returns an HttpClient carrying it as a
    /// Bearer token.
    /// </summary>
    public async Task<(HttpClient Client, string PlaintextToken, int InstallTokenId)> CreateBoldClientAsync(string name = "test-install")
    {
        var plaintext = BoldTokenHasher.GenerateToken();
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>();
            var entity = new BoldInstallToken
            {
                Name = name,
                TokenHash = BoldTokenHasher.Hash(plaintext),
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.BoldInstallTokens.Add(entity);
            await db.SaveChangesAsync();

            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", plaintext);
            return (client, plaintext, entity.Id);
        }
    }

    public async Task RevokeBoldTokenAsync(int installTokenId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>();
        var entity = await db.BoldInstallTokens.FindAsync(installTokenId);
        if (entity is not null)
        {
            entity.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Claims", out var roleHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var roles = roleHeader.ToString().Split(',', StringSplitOptions.TrimEntries);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user") };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Routes outgoing HTTP calls for a named provider client to a per-test override, defaulting to a
/// generic 200 OK so tests that don't care about provider behavior (health checks, auth tests)
/// don't need per-test setup.
/// </summary>
public class DelegatingTestHandler(Func<Func<HttpRequestMessage, HttpResponseMessage>?> resolveHandler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var handler = resolveHandler();
        var response = handler is not null
            ? handler(request)
            : new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent("{\"id\":\"fake\",\"output\":[],\"content\":[],\"usage\":{\"input_tokens\":0,\"output_tokens\":0}}", System.Text.Encoding.UTF8, "application/json"),
            };
        return Task.FromResult(response);
    }
}
