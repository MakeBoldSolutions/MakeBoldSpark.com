using MakeBoldSpark.Api.Features.Auth;
using MakeBoldSpark.Core.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace MakeBoldSpark.Api.Tests.Features.Auth;

[TestClass]
public class AuthServiceTests
{
    [TestMethod]
    public async Task Login_UsesTimingSafeHashForUnknownAndNonAdminAuthors()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MakeBoldSparkCoreDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new MakeBoldSparkCoreDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var nonAdmin = new Author
        {
            Email = "non-admin@example.test",
            Password = "not-used-by-recording-hasher",
            DisplayName = "Non-admin",
            IsAdmin = false,
        };
        db.Authors.Add(nonAdmin);
        await db.SaveChangesAsync();

        var hasher = new RecordingPasswordHasher();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = "test-signing-key" })
            .Build();
        var service = new AuthService(db, configuration, NullLogger<AuthService>.Instance, hasher);

        await service.LoginAsync(new LoginRequest { Email = "unknown@example.test", Password = "password" });
        await service.LoginAsync(new LoginRequest { Email = nonAdmin.Email, Password = "password" });

        Assert.AreEqual(2, hasher.VerifiedAuthors.Count);
        Assert.IsTrue(hasher.VerifiedAuthors.All(author =>
            author.Email == "timing-safe-placeholder@example.invalid"));
    }

    private sealed class RecordingPasswordHasher : IPasswordHasher<Author>
    {
        public List<Author> VerifiedAuthors { get; } = [];

        public string HashPassword(Author user, string password) => "not-used";

        public PasswordVerificationResult VerifyHashedPassword(Author user, string hashedPassword, string providedPassword)
        {
            VerifiedAuthors.Add(user);
            return PasswordVerificationResult.Failed;
        }
    }
}
