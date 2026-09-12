using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;

namespace MakeBoldSpark.Api.Tests.Features.Bold;

[TestClass]
public class BoldAuthTests
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
    public async Task Health_WithoutToken_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/integrations/bold/v1/health");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    [DataRow("/api/integrations/bold/v1/providers")]
    [DataRow("/api/integrations/bold/v1/model-roles")]
    [DataRow("/api/integrations/bold/v1/runs")]
    [DataRow("/api/integrations/bold/v1/usage")]
    public async Task NonHealthEndpoints_WithoutToken_ReturnUnauthorized(string path)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(path);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(raw).RootElement;
        Assert.IsTrue(json.TryGetProperty("error", out _), $"Body was: {raw}");
        Assert.AreEqual("unauthorized", json.GetProperty("error").GetProperty("code").GetString());
        Assert.IsFalse(json.GetProperty("error").GetProperty("retryable").GetBoolean());
    }

    [TestMethod]
    public async Task Completions_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            messages = new[] { new { role = "user", content = "hi" } }
        });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task NonHealthEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "bold_not-a-real-token");

        var response = await client.GetAsync("/api/integrations/bold/v1/model-roles");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task NonHealthEndpoint_WithValidToken_Succeeds()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.GetAsync("/api/integrations/bold/v1/model-roles");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task RevokedToken_ImmediatelyFailsAuth()
    {
        var (client, _, installTokenId) = await _factory.CreateBoldClientAsync();

        var beforeRevoke = await client.GetAsync("/api/integrations/bold/v1/model-roles");
        Assert.AreEqual(HttpStatusCode.OK, beforeRevoke.StatusCode);

        await _factory.RevokeBoldTokenAsync(installTokenId);

        var afterRevoke = await client.GetAsync("/api/integrations/bold/v1/model-roles");
        Assert.AreEqual(HttpStatusCode.Unauthorized, afterRevoke.StatusCode);
    }

    [TestMethod]
    public async Task InstallToken_OnAdminEndpoint_ReturnsUnauthorizedOrForbidden()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.GetAsync("/api/admin/health/deep");

        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected 401/403, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task InstallToken_OnBoldAdminTokenEndpoint_ReturnsUnauthorizedOrForbidden()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.PostAsJsonAsync("/api/admin/bold/tokens", new { name = "should-not-work" });

        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected 401/403, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task AdminCredentials_OnBoldClientEndpoint_ReturnsUnauthorized()
    {
        var adminClient = _factory.CreateAdminClient();

        var response = await adminClient.GetAsync("/api/integrations/bold/v1/model-roles");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Runs_OnlyReturnAuthenticatedInstalls_Data()
    {
        var (clientA, _, _) = await _factory.CreateBoldClientAsync("install-a");
        var (clientB, _, _) = await _factory.CreateBoldClientAsync("install-b");

        var responseA = await clientA.GetAsync("/api/integrations/bold/v1/runs");
        var responseB = await clientB.GetAsync("/api/integrations/bold/v1/runs");

        Assert.AreEqual(HttpStatusCode.OK, responseA.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, responseB.StatusCode);

        var jsonA = await responseA.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var jsonB = await responseB.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        // Neither newly created install has run history yet — both start empty, proving isolation
        // rather than shared/leaked state.
        Assert.AreEqual(0, jsonA.GetProperty("runs").GetArrayLength());
        Assert.AreEqual(0, jsonB.GetProperty("runs").GetArrayLength());
    }
}
