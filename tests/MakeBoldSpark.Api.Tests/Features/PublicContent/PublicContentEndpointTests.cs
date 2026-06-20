using System.Net;
using System.Text.Json;
using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MakeBoldSpark.Api.Tests.Features.PublicContent;

[TestClass]
public class PublicContentEndpointTests
{
    private static MakeBoldSparkWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _factory = new MakeBoldSparkWebApplicationFactory();
        await _factory.InitializeAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _factory.DisposeAsync();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _client = _factory.CreateClient();
    }

    [TestMethod]
    public async Task GetArticles_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/public/content/articles");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetArticles_ReturnsOnlyPublished()
    {
        var response = await _client.GetAsync("/api/public/content/articles");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var articles = doc.RootElement.EnumerateArray().ToList();

        foreach (var a in articles)
        {
            Assert.IsTrue(a.TryGetProperty("slug", out _));
            Assert.IsTrue(a.TryGetProperty("title", out _));
            Assert.IsTrue(a.TryGetProperty("summary", out _));
            Assert.IsTrue(a.TryGetProperty("tags", out _));
            Assert.IsFalse(a.TryGetProperty("body", out _), "Body should not appear in list response");
        }

        Assert.IsFalse(articles.Any(a => a.GetProperty("slug").GetString() == "draft-article"));
    }

    [TestMethod]
    public async Task GetArticleBySlug_Published_ReturnsFullDetail()
    {
        var response = await _client.GetAsync("/api/public/content/articles/hello-world");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.IsTrue(doc.RootElement.TryGetProperty("body", out var body));
        Assert.IsFalse(string.IsNullOrEmpty(body.GetString()));
    }

    [TestMethod]
    public async Task GetArticleBySlug_Draft_Returns404()
    {
        var response = await _client.GetAsync("/api/public/content/articles/draft-article");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetArticleBySlug_Nonexistent_Returns404()
    {
        var response = await _client.GetAsync("/api/public/content/articles/does-not-exist");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetArticleBySlug_InvalidSlug_Returns400()
    {
        var response = await _client.GetAsync("/api/public/content/articles/INVALID_SLUG!");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task GetTags_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/public/content/tags");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
