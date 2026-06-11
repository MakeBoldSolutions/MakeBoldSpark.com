using ApiSpark.Api.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using WebSpark.Core.Data;
using WebSpark.Recipe.Data;

namespace ApiSpark.Api.Infrastructure.Data;

public static class DatabaseSetup
{
    public static async Task InitializeAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILogger<ApiSparkDbContext>>();

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiSparkDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var apiSparkDbReady = false;

        if (config.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            var connStr = config.GetConnectionString("DefaultConnection") ?? "";
            var dataSource = ExtractDataSource(connStr);

            if (!string.IsNullOrEmpty(dataSource) && !dataSource.Contains(":memory:"))
            {
                var dir = Path.GetDirectoryName(dataSource);
                if (!string.IsNullOrEmpty(dir))
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                        var testFile = Path.Combine(dir, ".write-test");
                        await File.WriteAllTextAsync(testFile, "", cancellationToken);
                        File.Delete(testFile);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Database directory '{Dir}' is not writable — startup continues in degraded mode", dir);
                        return;
                    }
                }
            }

            try
            {
                await db.Database.MigrateAsync(cancellationToken);
                // WAL mode must be set via PRAGMA — not a valid Microsoft.Data.Sqlite connection string keyword
                await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
                logger.LogInformation("Database migrations applied and WAL mode enabled");
                apiSparkDbReady = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database migration failed — startup continues in degraded mode");
            }

            using var recipeScope = app.Services.CreateScope();
            var recipeDb = recipeScope.ServiceProvider.GetRequiredService<RecipeDbContext>();
            var recipeConnStr = config.GetConnectionString("RecipeConnection") ?? "";
            try
            {
                if (IsInMemory(recipeConnStr))
                {
                    await recipeDb.Database.EnsureCreatedAsync(cancellationToken);
                    logger.LogInformation("Recipe database schema created (in-memory)");
                }
                else
                {
                    await PreMarkAppliedIfTablesExistAsync(recipeDb, logger, cancellationToken);
                    await recipeDb.Database.MigrateAsync(cancellationToken);
                    await recipeDb.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
                    logger.LogInformation("Recipe database migrations applied");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Recipe database initialization failed — startup continues in degraded mode");
            }

            using var webSparkScope = app.Services.CreateScope();
            var webSparkDb = webSparkScope.ServiceProvider.GetRequiredService<WebSparkDbContext>();
            var webSparkConnStr = config.GetConnectionString("WebSparkConnection") ?? "";
            try
            {
                if (IsInMemory(webSparkConnStr))
                {
                    await webSparkDb.Database.EnsureCreatedAsync(cancellationToken);
                    logger.LogInformation("WebSpark database schema created (in-memory)");
                }
                else
                {
                    await PreMarkAppliedIfTablesExistAsync(webSparkDb, logger, cancellationToken);
                    await webSparkDb.Database.MigrateAsync(cancellationToken);
                    await webSparkDb.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
                    logger.LogInformation("WebSpark database migrations applied");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WebSpark database initialization failed — startup continues in degraded mode");
            }
        }

        if (config.GetValue<bool>("Database:SeedOnStartup") && apiSparkDbReady)
        {
            try
            {
                if (!await db.Articles.AnyAsync(cancellationToken))
                {
                    await SeedData.LoadAsync(db, cancellationToken);
                    logger.LogInformation("Seed data loaded");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Seed data load failed — startup continues in degraded mode");
            }
        }
    }

    private static string ExtractDataSource(string connStr)
    {
        foreach (var part in connStr.Split(';'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
                return kv[1].Trim();
        }
        return string.Empty;
    }

    private static bool IsInMemory(string connStr)
        => connStr.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase)
        || connStr.Contains(":memory:", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// If there are pending migrations but their tables already exist (created by a prior context),
    /// insert those migration IDs into __EFMigrationsHistory so MigrateAsync skips them cleanly.
    /// </summary>
    private static async Task PreMarkAppliedIfTablesExistAsync(DbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0) return;

        // Check whether the first migration's sentinel table already exists in sqlite_master
        var tableCount = await db.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL);",
            cancellationToken);

        // Use sqlite_master to detect pre-existing tables
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync(cancellationToken);
        int existingTableCount;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE '__EF%' AND name NOT LIKE 'sqlite_%';";
            existingTableCount = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
        }
        await conn.CloseAsync();

        if (existingTableCount == 0) return;

        // Tables exist from a prior system — mark all pending migrations as already applied
        foreach (var migrationId in pending)
        {
            await db.Database.ExecuteSqlAsync(
                $"INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({migrationId}, '10.0.0')",
                cancellationToken);
        }
        logger.LogInformation("Database: {Count} pending migration(s) pre-marked as applied (tables already existed)", pending.Count);
    }
}
