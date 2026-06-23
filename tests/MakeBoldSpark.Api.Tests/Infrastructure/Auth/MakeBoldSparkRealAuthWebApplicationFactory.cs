using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Core.Data;
using MakeBoldSpark.Recipe.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MakeBoldSpark.Api.Tests.Infrastructure.Auth;

/// <summary>
/// Unlike <see cref="MakeBoldSparkWebApplicationFactory"/>, this factory does NOT replace JWT
/// Bearer authentication with a test-only scheme — it configures a real <c>Jwt:SigningKey</c>
/// and leaves <c>AuthorizationSetup.AddMakeBoldSparkAuth</c>'s real signature-validation path
/// active, so tests can exercise the actual tightened auth pipeline (T008a, T016-T019b, T019).
/// See plan.md's Implementation Notes (2026-06-22) for why the existing factory cannot be
/// reused for this purpose.
/// </summary>
public class MakeBoldSparkRealAuthWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "test-only-signing-key-do-not-use-in-production-0123456789";

    /// <summary>Every log message captured during the test run — used by T019b to assert the
    /// login route's request body/submitted password is never captured by request logging.</summary>
    public List<string> CapturedLogs { get; } = [];

    private SqliteConnection? _connection;
    private SqliteConnection? _recipeConnection;
    private SqliteConnection? _makeBoldSparkConnection;

    private readonly string _dbName = $"RealAuthTestDb_{Guid.NewGuid():N}";
    private readonly string _recipeDbName = $"RealAuthRecipeTestDb_{Guid.NewGuid():N}";
    private readonly string _makeBoldSparkDbName = $"RealAuthMakeBoldSparkTestDb_{Guid.NewGuid():N}";

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
        if (_connection is not null) await _connection.DisposeAsync();
        if (_recipeConnection is not null) await _recipeConnection.DisposeAsync();
        if (_makeBoldSparkConnection is not null) await _makeBoldSparkConnection.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbName};Mode=Memory;Cache=Shared;",
                ["ConnectionStrings:RecipeConnection"] = $"Data Source={_recipeDbName};Mode=Memory;Cache=Shared;",
                ["ConnectionStrings:MakeBoldSparkConnection"] = $"Data Source={_makeBoldSparkDbName};Mode=Memory;Cache=Shared;",
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["Database:SeedOnStartup"] = "false",
                ["Jwt:SigningKey"] = TestSigningKey,
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MakeBoldSparkDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddDbContext<MakeBoldSparkDbContext>(options =>
                options.UseSqlite($"Data Source={_dbName};Mode=Memory;Cache=Shared;"));

            var recipeDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RecipeDbContext>));
            if (recipeDescriptor is not null) services.Remove(recipeDescriptor);
            services.AddDbContext<RecipeDbContext>(options =>
                options.UseSqlite($"Data Source={_recipeDbName};Mode=Memory;Cache=Shared;"));

            var makeBoldSparkDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MakeBoldSparkCoreDbContext>));
            if (makeBoldSparkDescriptor is not null) services.Remove(makeBoldSparkDescriptor);
            services.AddDbContext<MakeBoldSparkCoreDbContext>(options =>
                options.UseSqlite($"Data Source={_makeBoldSparkDbName};Mode=Memory;Cache=Shared;")
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

            services.AddLogging(b => b.AddProvider(new CapturingLoggerProvider(CapturedLogs)));
        });
    }

    /// <summary>Inserts an Author row with a real PBKDF2 hash via <see cref="PasswordHasher{TUser}"/>, for login tests.</summary>
    public async Task<Author> SeedAuthorAsync(string email, string password, bool isAdmin, string displayName = "Test Author")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MakeBoldSparkCoreDbContext>();

        var author = new Author
        {
            Email = email,
            Password = string.Empty,
            DisplayName = displayName,
            IsAdmin = isAdmin,
        };
        author.Password = new PasswordHasher<Author>().HashPassword(author, password);

        db.Authors.Add(author);
        await db.SaveChangesAsync();
        return author;
    }
}

internal sealed class CapturingLoggerProvider(List<string> sink) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, sink);

    public void Dispose() { }

    private sealed class CapturingLogger(string categoryName, List<string> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (sink)
            {
                sink.Add($"[{categoryName}] {formatter(state, exception)}");
            }
        }
    }
}
