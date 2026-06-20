using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MakeBoldSpark.Core.Data;

/// <summary>
/// Db Initializer
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Seed the Db if needed
    /// </summary>
    /// <param name="applicationBuilder"></param>
    public static async Task SeedAsync(IApplicationBuilder applicationBuilder)
    {
        using MakeBoldSparkCoreDbContext cmsCtx = applicationBuilder.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<MakeBoldSparkCoreDbContext>();
        if (!cmsCtx.Database.CanConnect())
        {
            await cmsCtx.Database.EnsureDeletedAsync();
            await cmsCtx.Database.EnsureCreatedAsync();
            var seedDatabase = new SeedDatabase(cmsCtx);
            await seedDatabase.SeedDatabaseAsync();
        }

    }
}
