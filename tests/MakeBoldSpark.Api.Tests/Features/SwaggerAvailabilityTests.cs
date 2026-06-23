using System.Net;
using System.Text.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace MakeBoldSpark.Api.Tests.Features;

[TestClass]
public class ApiDocsAvailabilityTests
{
    [TestMethod]
    public async Task ApiTestSparkUi_InDevelopment_ReturnsOk()
    {
        await using var factory = new MakeBoldSparkWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api-test-spark/");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task OpenApiJson_InDevelopment_ReturnsOk()
    {
        await using var factory = new MakeBoldSparkWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task OpenApiJson_InDevelopment_IncludesApiTestSparkOptimizedMetadata()
    {
        await using var factory = new MakeBoldSparkWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        var tagNames = root.GetProperty("tags").EnumerateArray()
            .Select(tag => tag.GetProperty("name").GetString())
            .ToList();

        CollectionAssert.IsSubsetOf(new[]
        {
            "Health: Diagnostics",
            "Public Content: Articles",
            "Recipes: Catalog",
            "CMS Public: Domains",
            "CMS Admin: Posts",
            "CMS Public: Authors",
            "CMS Messaging: Newsletters",
            "Async Demo: Weather Patterns"
        }, tagNames);

        var description = root.GetProperty("info").GetProperty("description").GetString();
        StringAssert.Contains(description, "Suggested workflow");

        var schemas = root.GetProperty("components").GetProperty("schemas");
        var articleSummary = schemas.GetProperty("ArticleSummary");
        Assert.AreEqual("URL-safe identifier unique across all published articles.",
            articleSummary.GetProperty("properties").GetProperty("slug").GetProperty("description").GetString());

        Assert.IsTrue(schemas.TryGetProperty("WeatherRetryResponse", out _),
            "Weather retry endpoint should expose a concrete response schema instead of anonymous object metadata.");

        var loopCount = schemas.GetProperty("MockResultsRequest").GetProperty("properties").GetProperty("loopCount");
        Assert.AreEqual(1, loopCount.GetProperty("minimum").GetInt32());
        Assert.AreEqual("Number of loop iterations the mock service will attempt to execute.",
            loopCount.GetProperty("description").GetString());
    }

    [TestMethod]
    public async Task ApiTestSparkUi_InProduction_ReturnsOk()
    {
        using var _ = TestSigningKeyEnvironmentVariable.Set();
        await using var factory = new ProductionWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api-test-spark/");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // OpenAPI JSON is intentionally served in all environments for this public demo API.
    // Restrict via network-level controls if production exposure is undesired.
    [TestMethod]
    public async Task OpenApiJson_InProduction_ReturnsOk()
    {
        using var _ = TestSigningKeyEnvironmentVariable.Set();
        await using var factory = new ProductionWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}

/// <summary>
/// AuthorizationSetup.AddMakeBoldSparkAuth now fails fast without a signing key (tasks.md
/// T009), and it reads configuration before WebApplicationFactory's ConfigureAppConfiguration
/// hooks reliably apply to a minimal-hosting-model Program.cs. An environment variable, which
/// WebApplication.CreateBuilder always reads as part of its default configuration sources, is
/// the reliable way to supply a throwaway key to this availability-only test fixture.
/// </summary>
internal sealed class TestSigningKeyEnvironmentVariable : IDisposable
{
    private const string VariableName = "Jwt__SigningKey";

    private TestSigningKeyEnvironmentVariable(string? originalValue) => OriginalValue = originalValue;

    private string? OriginalValue { get; }

    public static TestSigningKeyEnvironmentVariable Set()
    {
        var originalValue = Environment.GetEnvironmentVariable(VariableName);
        Environment.SetEnvironmentVariable(VariableName, "swagger-availability-test-fixture-signing-key-0000");
        return new TestSigningKeyEnvironmentVariable(originalValue);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(VariableName, OriginalValue);
}

internal class ProductionWebApplicationFactory : MakeBoldSparkWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Production");
    }
}
