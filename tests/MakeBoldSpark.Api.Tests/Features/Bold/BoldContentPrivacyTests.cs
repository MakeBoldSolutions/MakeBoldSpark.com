using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Infrastructure.Observability;
using MakeBoldSpark.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MakeBoldSpark.Api.Tests.Features.Bold;

/// <summary>
/// spec.md Content Privacy clause (critic blocker fix): the server is a forwarder, not a store, of
/// workspace content. Message bodies and provider output text must never persist to the database
/// or reach the request-logging middleware (AC6, AC10).
/// </summary>
[TestClass]
public class BoldContentPrivacyTests
{
    private static MakeBoldSparkWebApplicationFactory _factory = null!;
    private const string SecretUserMessage = "SUPER-SECRET-USER-PROMPT-CONTENT-9f3a";
    private const string SecretProviderOutput = "SUPER-SECRET-PROVIDER-OUTPUT-7c21";

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
    public void RequestLoggingMiddleware_IdentifiesBoldRoutes()
    {
        Assert.IsTrue(RequestLoggingMiddleware.IsBoldRoute("/api/integrations/bold/v1/completions"));
        Assert.IsFalse(RequestLoggingMiddleware.IsBoldRoute("/api/public/content/articles"));
    }

    [TestMethod]
    public async Task Completion_MessageAndOutputContent_NeverPersistedToRunTable()
    {
        _factory.OpenAiHandler = _ =>
        {
            var body = JsonSerializer.Serialize(new
            {
                id = "resp_privacy",
                output = new[] { new { type = "message", content = new[] { new { type = "output_text", text = SecretProviderOutput } } } },
                usage = new { input_tokens = 3, output_tokens = 3 },
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        };

        var (client, _, installTokenId) = await _factory.CreateBoldClientAsync("privacy-install");

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            messages = new[] { new { role = "user", content = SecretUserMessage } },
        });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        // The API response to the caller legitimately contains the content (it's the actual answer) —
        // what must never happen is that content landing in the database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>();
        var run = await db.BoldRuns.AsNoTracking().SingleAsync(r => r.InstallTokenId == installTokenId);

        var runJson = JsonSerializer.Serialize(run);
        Assert.IsFalse(runJson.Contains(SecretUserMessage), "Run record must not contain the request message text.");
        Assert.IsFalse(runJson.Contains(SecretProviderOutput), "Run record must not contain the provider output text.");
    }
    [TestMethod]
    [DataRow("router", false)]
    [DataRow("router", true)]
    [DataRow("planner", false)]
    [DataRow("planner", true)]
    public async Task ProviderCalls_DoNotLogOrPersistContent(string role, bool failure)
    {
        await using var factory = new MakeBoldSparkWebApplicationFactory();
        await factory.InitializeAsync();
        HttpResponseMessage Reply(HttpRequestMessage _) => new(failure ? HttpStatusCode.BadRequest : HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                id = "fictional-privacy-response",
                output = new[] { new { content = new[] { new { type = "output_text", text = SecretProviderOutput } } } },
                content = new[] { new { type = "text", text = SecretProviderOutput } },
                error = new { message = SecretUserMessage + SecretProviderOutput },
                usage = new { input_tokens = 1, output_tokens = 1 }
            }), System.Text.Encoding.UTF8, "application/json")
        };
        factory.OpenAiHandler = Reply;
        factory.AnthropicHandler = Reply;
        var (client, _, id) = await factory.CreateBoldClientAsync();
        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = role,
            messages = new[] { new { role = "user", content = SecretUserMessage } }
        });
        Assert.AreEqual(failure ? HttpStatusCode.BadGateway : HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>();
        var stored = JsonSerializer.Serialize(await db.BoldRuns.SingleAsync(r => r.InstallTokenId == id));
        string logs;
        lock (factory.CapturedLogs) logs = string.Join("\n", factory.CapturedLogs);
        Assert.IsTrue(logs.Contains("RequestLoggingMiddleware"), "Capture must include the actual request pipeline.");
        foreach (var secret in new[] { SecretUserMessage, SecretProviderOutput })
        {
            Assert.IsFalse(logs.Contains(secret), "Content reached application logging.");
            Assert.IsFalse(stored.Contains(secret), "Content reached persisted metadata.");
        }
    }

}
