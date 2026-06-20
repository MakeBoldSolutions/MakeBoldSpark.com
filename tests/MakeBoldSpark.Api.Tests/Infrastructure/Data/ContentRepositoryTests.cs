using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Infrastructure.Data.Entities;
using MakeBoldSpark.Api.Infrastructure.Data.Repositories;
using MakeBoldSpark.Api.Infrastructure.Data.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MakeBoldSpark.Api.Tests.Infrastructure.Data;

[TestClass]
public class ContentRepositoryTests
{
    private SqliteConnection _connection = null!;
    private MakeBoldSparkDbContext _db = null!;
    private ContentRepository _repo = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        var dbName = $"RepoTest_{Guid.NewGuid():N}";
        _connection = new SqliteConnection($"Data Source={dbName};Mode=Memory;Cache=Shared;");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MakeBoldSparkDbContext>()
            .UseSqlite($"Data Source={dbName};Mode=Memory;Cache=Shared;")
            .Options;

        _db = new MakeBoldSparkDbContext(options);
        await _db.Database.EnsureCreatedAsync();
        await SeedData.LoadAsync(_db);

        _repo = new ContentRepository(_db, NullLogger<ContentRepository>.Instance);
    }

    [TestCleanup]
    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [TestMethod]
    public async Task GetPublishedArticles_ReturnsOnlyPublished()
    {
        var articles = await _repo.GetPublishedArticlesAsync();
        foreach (var article in articles)
        {
            Assert.IsFalse(string.IsNullOrEmpty(article.Slug));
        }

        Assert.IsFalse(articles.Any(a => a.Slug == "draft-article"));
        Assert.IsTrue(articles.Any(a => a.Slug == "hello-world"));
    }

    [TestMethod]
    public async Task GetPublishedArticles_ExcludesBody()
    {
        var articles = await _repo.GetPublishedArticlesAsync();
        Assert.IsTrue(articles.Count >= 2);
    }

    [TestMethod]
    public async Task GetPublishedArticleBySlug_Published_ReturnsDetail()
    {
        var article = await _repo.GetPublishedArticleBySlugAsync("hello-world");
        Assert.IsNotNull(article);
        Assert.AreEqual("hello-world", article.Slug);
        Assert.IsFalse(string.IsNullOrEmpty(article.Body));
    }

    [TestMethod]
    public async Task GetPublishedArticleBySlug_Draft_ReturnsNull()
    {
        var article = await _repo.GetPublishedArticleBySlugAsync("draft-article");
        Assert.IsNull(article);
    }

    [TestMethod]
    public async Task GetPublishedArticleBySlug_Nonexistent_ReturnsNull()
    {
        var article = await _repo.GetPublishedArticleBySlugAsync("does-not-exist");
        Assert.IsNull(article);
    }

    [TestMethod]
    public async Task GetAllTags_ReturnsTags()
    {
        var tags = await _repo.GetAllTagsAsync();
        Assert.IsTrue(tags.Count >= 2);
        Assert.IsTrue(tags.Any(t => t.Name == "general"));
    }
}
