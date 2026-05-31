using System.Net;
using System.Text.Json;
using ApiSpark.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace ApiSpark.Api.Tests.Features;

[TestClass]
public class ApiDocsAvailabilityTests
{
    [TestMethod]
    public async Task ScalarUi_InDevelopment_ReturnsOk()
    {
        await using var factory = new ApiSparkWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/scalar/v1");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task OpenApiJson_InDevelopment_ReturnsOk()
    {
        await using var factory = new ApiSparkWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task OpenApiJson_InDevelopment_IncludesApiTestSparkOptimizedMetadata()
    {
        await using var factory = new ApiSparkWebApplicationFactory();
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
    public async Task ScalarUi_InProduction_ReturnsNotFound()
    {
        await using var factory = new ProductionWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/scalar/v1");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // OpenAPI JSON is intentionally served in all environments for this public demo API.
    // Restrict via network-level controls if production exposure is undesired.
    [TestMethod]
    public async Task OpenApiJson_InProduction_ReturnsOk()
    {
        await using var factory = new ProductionWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}

internal class ProductionWebApplicationFactory : ApiSparkWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Production");
    }
}
