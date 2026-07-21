using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Infrastructure.Data.Entities;
using MakeBoldSpark.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MakeBoldSpark.Api.Tests.Features.Bold;

[TestClass]
public class BoldRunsUsageTests
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

    private static async Task SeedRunAsync(int installTokenId, string provider, string model, string modelRole,
        string? workflow, string? workspaceId, int inputTokens, int outputTokens, decimal cost, DateTimeOffset? createdAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>();
        db.BoldRuns.Add(new BoldRun
        {
            RunId = Guid.NewGuid().ToString("n"),
            InstallTokenId = installTokenId,
            Provider = provider,
            Model = model,
            ModelRole = modelRole,
            Workflow = workflow,
            WorkspaceId = workspaceId,
            Status = "succeeded",
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            EstimatedCostUsd = cost,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    [TestMethod]
    public async Task GetRunById_UnknownId_ReturnsNotFound()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync("runs-notfound");

        var response = await client.GetAsync("/api/integrations/bold/v1/runs/does-not-exist");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("not_found", json.GetProperty("error").GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task GetRuns_FiltersByWorkspaceAndWorkflow()
    {
        var (client, _, installTokenId) = await _factory.CreateBoldClientAsync("runs-filter");

        await SeedRunAsync(installTokenId, "openai", "gpt-5.1", "reviewer", "build", "ws-a", 10, 5, 0.01m);
        await SeedRunAsync(installTokenId, "openai", "gpt-5.1", "reviewer", "plan", "ws-b", 10, 5, 0.01m);

        var response = await client.GetAsync("/api/integrations/bold/v1/runs?workspace_id=ws-a");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var runs = json.GetProperty("runs");
        Assert.AreEqual(1, runs.GetArrayLength());
        Assert.AreEqual("ws-a", runs[0].GetProperty("workspace_id").GetString());
    }

    [TestMethod]
    public async Task GetRuns_Pagination_ReachesFinalPageWithNullCursor()
    {
        var (client, _, installTokenId) = await _factory.CreateBoldClientAsync("runs-paging");

        for (var i = 0; i < 5; i++)
            await SeedRunAsync(installTokenId, "openai", "gpt-5.1", "router", null, null, 1, 1, 0.001m);

        var firstPage = await client.GetAsync("/api/integrations/bold/v1/runs?limit=2");
        Assert.AreEqual(HttpStatusCode.OK, firstPage.StatusCode);
        var firstJson = await firstPage.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(2, firstJson.GetProperty("runs").GetArrayLength());
        var cursor1 = firstJson.GetProperty("next_cursor").GetString();
        Assert.IsNotNull(cursor1);

        var secondPage = await client.GetAsync($"/api/integrations/bold/v1/runs?limit=2&cursor={Uri.EscapeDataString(cursor1!)}");
        var secondJson = await secondPage.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(2, secondJson.GetProperty("runs").GetArrayLength());
        var cursor2 = secondJson.GetProperty("next_cursor").GetString();
        Assert.IsNotNull(cursor2);

        var thirdPage = await client.GetAsync($"/api/integrations/bold/v1/runs?limit=2&cursor={Uri.EscapeDataString(cursor2!)}");
        var thirdJson = await thirdPage.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(1, thirdJson.GetProperty("runs").GetArrayLength());
        Assert.AreEqual(JsonValueKind.Null, thirdJson.GetProperty("next_cursor").ValueKind);
    }

    [TestMethod]
    public async Task GetUsage_AggregatesTotalsAndGroupsByProvider()
    {
        var (client, _, installTokenId) = await _factory.CreateBoldClientAsync("usage-agg");

        await SeedRunAsync(installTokenId, "openai", "gpt-5.1", "reviewer", null, null, 100, 50, 0.10m);
        await SeedRunAsync(installTokenId, "anthropic", "claude-sonnet-4-5", "planner", null, null, 200, 100, 0.20m);

        var response = await client.GetAsync("/api/integrations/bold/v1/usage?group_by=provider");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.AreEqual(300, json.GetProperty("totals").GetProperty("input_tokens").GetInt32());
        Assert.AreEqual(150, json.GetProperty("totals").GetProperty("output_tokens").GetInt32());

        var breakdown = json.GetProperty("breakdown");
        Assert.AreEqual(2, breakdown.GetArrayLength());
    }
}
