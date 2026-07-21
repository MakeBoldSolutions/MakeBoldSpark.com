using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;

namespace MakeBoldSpark.Api.Tests.Features.Bold;

[TestClass]
public class BoldStatusEndpointTests
{
    private static MakeBoldSparkWebApplicationFactory _factory = null!;

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

    [TestMethod]
    public async Task Health_ReturnsContractShape()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/integrations/bold/v1/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("ok", json.GetProperty("status").GetString());
        Assert.IsFalse(string.IsNullOrEmpty(json.GetProperty("version").GetString()));
        Assert.IsTrue(json.TryGetProperty("time", out _));
    }

    [TestMethod]
    public async Task Providers_ReturnsContractShape_WithoutCredentials()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync();
        var response = await client.GetAsync("/api/integrations/bold/v1/providers");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var providers = json.GetProperty("providers");
        Assert.IsTrue(providers.GetArrayLength() >= 2);

        foreach (var provider in providers.EnumerateArray())
        {
            Assert.IsTrue(provider.TryGetProperty("provider", out _));
            Assert.IsTrue(provider.TryGetProperty("reachable", out _));
            var raw = provider.GetRawText();
            Assert.IsFalse(raw.Contains("api_key", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(raw.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        }
    }

    [TestMethod]
    public async Task ModelRoles_ReturnsConfiguredRouting()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync();
        var response = await client.GetAsync("/api/integrations/bold/v1/model-roles");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var roles = json.GetProperty("roles");
        Assert.IsTrue(roles.GetArrayLength() >= 3);

        var roleNames = roles.EnumerateArray().Select(r => r.GetProperty("role").GetString()).ToList();
        CollectionAssert.Contains(roleNames, "router");
        CollectionAssert.Contains(roleNames, "planner");
        CollectionAssert.Contains(roleNames, "reviewer");
    }
}
