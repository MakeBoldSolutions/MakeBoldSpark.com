using System.Diagnostics;
using System.Net;
using System.Text.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MakeBoldSpark.Api.Tests.Features.Health;

[TestClass]
public class HealthEndpointTests
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
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetHealth_ReturnsCorrectBody()
    {
        var response = await _client.GetAsync("/api/health");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.AreEqual("Healthy", doc.RootElement.GetProperty("status").GetString());
        Assert.AreEqual("MakeBoldSpark", doc.RootElement.GetProperty("service").GetString());
        Assert.IsFalse(string.IsNullOrEmpty(doc.RootElement.GetProperty("version").GetString()));
    }

    [TestMethod]
    public async Task GetHealth_IsAnonymous_NoAuthRequired()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/api/health");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetHealth_RespondsFastEnough()
    {
        var sw = Stopwatch.StartNew();
        await _client.GetAsync("/api/health");
        sw.Stop();
        Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"Health response exceeded 500ms: {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public async Task GetDeepHealth_WithAdmin_IncludesRecipeDatabaseReadiness()
    {
        var adminClient = _factory.CreateAdminClient();

        var response = await adminClient.GetAsync("/api/admin/health/deep");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var checks = doc.RootElement.GetProperty("checks");
        Assert.AreEqual("ok", checks.GetProperty("database").GetString());
        Assert.AreEqual("ok", checks.GetProperty("recipeDatabase").GetString());
    }
}
