using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;

namespace MakeBoldSpark.Api.Tests.Features.Bold;

[TestClass]
public class BoldEmbeddingsTests
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
    public async Task Embeddings_AlwaysReturnsStableNotImplementedError()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/embeddings", new
        {
            input = new[] { "some text" },
        });

        Assert.AreEqual((HttpStatusCode)501, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("not_implemented", json.GetProperty("error").GetProperty("code").GetString());
        Assert.IsFalse(json.GetProperty("error").GetProperty("retryable").GetBoolean());
    }

    [TestMethod]
    public async Task Embeddings_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/embeddings", new { input = new[] { "x" } });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
