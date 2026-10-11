using MakeBoldSpark.Api.Features.FamilyMemories;
using MakeBoldSpark.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;


namespace MakeBoldSpark.Api.Tests.Features.FamilyMemories;

[TestClass]
public class FamilyMemoryMigrationTests
{
    [TestMethod]
    public async Task Upgrade_preserves_existing_data_and_memories_survive_database_reopen()
    {
        var path = Path.Combine(Path.GetTempPath(), $"family-memory-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MakeBoldSparkDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
        try
        {
            await using (var db = new MakeBoldSparkDbContext(options))
            {
                await db.GetService<IMigrator>().MigrateAsync("20260721145147_AddBoldFeature");
                await db.Database.ExecuteSqlRawAsync("INSERT INTO Tags (Name) VALUES ('Fictional existing tag')");
                await db.Database.MigrateAsync();
                Assert.IsFalse(db.Database.HasPendingModelChanges());
                Assert.AreEqual("Fictional existing tag", (await db.Tags.SingleAsync()).Name);
                Assert.HasCount(0, await db.FamilyMemories.ToListAsync());
                db.FamilyMemories.Add(new FamilyMemory { PersonName = "Fictional Zoë", PersonPronouns = "", RelationshipToJared = "Fictional friend", Body = "First.\r\n\r\n[**Fictional**](https://example.com) 🦊", IsPublished = true });
                await db.SaveChangesAsync();
            }
            await using (var reopened = new MakeBoldSparkDbContext(options))
            {
                await reopened.Database.MigrateAsync();
                var memory = await reopened.FamilyMemories.SingleAsync();
                Assert.AreEqual("First.\r\n\r\n[**Fictional**](https://example.com) 🦊", memory.Body);
                Assert.AreEqual("", memory.PersonPronouns);
                // Verify NOT NULL at the database boundary as well as HTTP validation.
                await Assert.ThrowsAsync<Exception>(() => reopened.Database.ExecuteSqlRawAsync("UPDATE FamilyMemories SET Body = NULL"));
                await reopened.GetService<IMigrator>().MigrateAsync("20260721145147_AddBoldFeature");
                Assert.AreEqual("Fictional existing tag", (await reopened.Tags.SingleAsync()).Name);
                await reopened.Database.MigrateAsync();
                Assert.HasCount(0, await reopened.FamilyMemories.ToListAsync());
            }
        }
        finally { File.Delete(path); }
    }
}

