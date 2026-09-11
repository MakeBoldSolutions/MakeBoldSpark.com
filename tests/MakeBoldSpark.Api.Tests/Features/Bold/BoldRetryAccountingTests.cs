using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MakeBoldSpark.Api.Features.Bold;
using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MakeBoldSpark.Api.Tests.Features.Bold;

[TestClass]
public class BoldRetryAccountingTests
{
    [TestMethod]
    [DataRow("success", 200, 300, 150)]
    [DataRow("schema", 422, 300, 150)]
    [DataRow("provider", 502, 100, 50)]
    public async Task AllKnownAttemptUsage_IsRecordedEvenOnFailure(string ending, int status, int input, int output)
    {
        await using var factory = new MakeBoldSparkWebApplicationFactory();
        await factory.InitializeAsync();
        var calls = 0;
        factory.OpenAiHandler = _ =>
        {
            calls++;
            if (ending == "provider" && calls == 2)
                return new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("{}") };
            var text = ending == "success" && calls == 3 ? "{}" : "fictional invalid JSON";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    id = "fictional-attempt",
                    output = new[] { new { content = new[] { new { type = "output_text", text } } } },
                    usage = new { input_tokens = 100, output_tokens = 50 }
                }), System.Text.Encoding.UTF8, "application/json")
            };
        };
        var (client, _, id) = await factory.CreateBoldClientAsync();
        factory.Services.GetRequiredService<IOptions<BoldOptions>>().Value.CostCap.MonthlyLimitUsd = 0.0003m;
        var body = new
        {
            model_role = "router",
            messages = new[] { new { role = "user", content = "Fictional accounting example" } },
            response_format = "json",
            response_schema = new { type = "object" }
        };
        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", body);
        Assert.AreEqual(status, (int)response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var run = await scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>().BoldRuns.SingleAsync(r => r.InstallTokenId == id);
        Assert.AreEqual(input, run.InputTokens);
        Assert.AreEqual(output, run.OutputTokens);
        Assert.AreEqual(input / 1_000_000m * 0.25m + output / 1_000_000m * 2m, run.EstimatedCostUsd);
        if (ending != "provider")
        {
            Assert.AreEqual(3, calls);
            var blocked = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", body);
            Assert.AreEqual(HttpStatusCode.TooManyRequests, blocked.StatusCode);
            var error = await blocked.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("cost_cap_exceeded", error.GetProperty("error").GetProperty("code").GetString());
            Assert.AreEqual(3, calls, "The accumulated cost must block further provider calls.");
        }
        if (ending == "success")
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(input, json.GetProperty("usage").GetProperty("input_tokens").GetInt32());
        }
    }
}
