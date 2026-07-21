using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;

namespace MakeBoldSpark.Api.Tests.Features.Bold;

[TestClass]
public class BoldCompletionsTests
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

    [TestInitialize]
    public void TestInitialize()
    {
        // Every test gets a clean slate: default success responses from both fake provider clients.
        _factory.OpenAiHandler = null;
        _factory.AnthropicHandler = null;
    }

    private static HttpResponseMessage TextResponse(string text, int inputTokens = 10, int outputTokens = 5)
    {
        var body = JsonSerializer.Serialize(new
        {
            id = "resp_1",
            output = new[] { new { type = "message", content = new[] { new { type = "output_text", text } } } },
            usage = new { input_tokens = inputTokens, output_tokens = outputTokens },
        });
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
    }

    private static HttpResponseMessage AnthropicTextResponse(string text, int inputTokens = 10, int outputTokens = 5)
    {
        var body = JsonSerializer.Serialize(new
        {
            id = "msg_1",
            content = new[] { new { type = "text", text } },
            usage = new { input_tokens = inputTokens, output_tokens = outputTokens },
        });
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
    }

    [TestMethod]
    public async Task RouterRole_RoutesToOpenAi()
    {
        _factory.OpenAiHandler = _ => TextResponse("hello from openai");
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            messages = new[] { new { role = "user", content = "hi" } },
        });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("openai", json.GetProperty("provider").GetString());
        Assert.AreEqual("hello from openai", json.GetProperty("content").GetString());
        Assert.IsTrue(json.GetProperty("usage").GetProperty("input_tokens").GetInt32() > 0);
    }

    [TestMethod]
    public async Task PlannerRole_RoutesToAnthropic()
    {
        _factory.AnthropicHandler = _ => AnthropicTextResponse("hello from anthropic");
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "planner",
            messages = new[] { new { role = "user", content = "hi" } },
        });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("anthropic", json.GetProperty("provider").GetString());
        Assert.AreEqual("hello from anthropic", json.GetProperty("content").GetString());
    }

    [TestMethod]
    public async Task ProviderOverride_IsHonored()
    {
        _factory.AnthropicHandler = _ => AnthropicTextResponse("via override");
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        // "router" defaults to openai; override to anthropic.
        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            provider = "anthropic",
            messages = new[] { new { role = "user", content = "hi" } },
        });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("anthropic", json.GetProperty("provider").GetString());
    }

    [TestMethod]
    public async Task JsonSchema_ValidOutput_Succeeds()
    {
        _factory.OpenAiHandler = _ => TextResponse("""{"name":"Mark"}""");
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            messages = new[] { new { role = "user", content = "give me json" } },
            response_format = "json",
            response_schema = new { type = "object", required = new[] { "name" }, properties = new { name = new { type = "string" } } },
        });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(0, json.GetProperty("retries").GetInt32());
    }

    [TestMethod]
    public async Task JsonSchema_InvalidOutput_RetriesThenReturns422()
    {
        var callCount = 0;
        _factory.OpenAiHandler = _ =>
        {
            callCount++;
            return TextResponse("""{"wrong":"shape"}"""); // never satisfies the schema
        };
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            messages = new[] { new { role = "user", content = "give me json" } },
            response_format = "json",
            response_schema = new { type = "object", required = new[] { "name" }, properties = new { name = new { type = "string" } } },
        });

        Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("schema_validation_failed", json.GetProperty("error").GetProperty("code").GetString());
        // Default MaxRetries is 2 -> 3 total attempts.
        Assert.AreEqual(3, callCount);
    }

    [TestMethod]
    public async Task ProviderOutage_ReturnsBadGateway()
    {
        _factory.OpenAiHandler = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("""{"error":{"message":"down"}}""", System.Text.Encoding.UTF8, "application/json"),
        };
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            messages = new[] { new { role = "user", content = "hi" } },
        });

        Assert.AreEqual(HttpStatusCode.BadGateway, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("provider_unavailable", json.GetProperty("error").GetProperty("code").GetString());
        Assert.IsTrue(json.GetProperty("error").GetProperty("retryable").GetBoolean());
    }

    [TestMethod]
    public async Task ProviderInvalidKey_ReturnsBadGateway_NotRetryable()
    {
        _factory.OpenAiHandler = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"error":{"message":"invalid key"}}""", System.Text.Encoding.UTF8, "application/json"),
        };
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            messages = new[] { new { role = "user", content = "hi" } },
        });

        Assert.AreEqual(HttpStatusCode.BadGateway, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("provider_invalid_key", json.GetProperty("error").GetProperty("code").GetString());
        Assert.IsFalse(json.GetProperty("error").GetProperty("retryable").GetBoolean());
    }

    [TestMethod]
    public async Task TooManyMessages_ReturnsBadRequest_BeforeProviderCall()
    {
        var called = false;
        _factory.OpenAiHandler = _ => { called = true; return TextResponse("should not be called"); };
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var messages = Enumerable.Range(0, 60).Select(_ => new { role = "user", content = "hi" }).ToArray();
        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            messages,
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("request_too_large", json.GetProperty("error").GetProperty("code").GetString());
        Assert.IsFalse(called, "Provider must never be called once request bounds are exceeded.");
    }

    [TestMethod]
    public async Task EmptyMessages_ReturnsBadRequest()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            messages = Array.Empty<object>(),
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task RateLimit_ExceededReturns429_WithRetryAfter()
    {
        _factory.OpenAiHandler = _ => TextResponse("ok");
        var (client, _, _) = await _factory.CreateBoldClientAsync("rate-limit-install");

        HttpResponseMessage? limited = null;
        // BoldOptions default is 30/minute; issue one more than that to trip the limiter.
        for (var i = 0; i < 31; i++)
        {
            var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
            {
                model_role = "router",
                messages = new[] { new { role = "user", content = "hi" } },
            });
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                limited = response;
                break;
            }
        }

        Assert.IsNotNull(limited, "Expected the 31st request within a minute to be rate limited.");
        Assert.IsTrue(limited!.Headers.Contains("Retry-After"));
        var json = await limited.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("rate_limit_exceeded", json.GetProperty("error").GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task UnknownModelRole_ReturnsBadRequest()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync();

        var response = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "not-a-real-role",
            messages = new[] { new { role = "user", content = "hi" } },
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task SuccessfulCompletion_IsRecordedInRunHistory()
    {
        _factory.OpenAiHandler = _ => TextResponse("recorded output");
        var (client, _, _) = await _factory.CreateBoldClientAsync("run-history-install");

        var completionResponse = await client.PostAsJsonAsync("/api/integrations/bold/v1/completions", new
        {
            model_role = "router",
            messages = new[] { new { role = "user", content = "hi" } },
            client_metadata = new { workspace_id = "ws-1", workflow = "build" },
        });
        Assert.AreEqual(HttpStatusCode.OK, completionResponse.StatusCode);
        var completionJson = await completionResponse.Content.ReadFromJsonAsync<JsonElement>();
        var runId = completionJson.GetProperty("run_id").GetString();

        var runResponse = await client.GetAsync($"/api/integrations/bold/v1/runs/{runId}");
        Assert.AreEqual(HttpStatusCode.OK, runResponse.StatusCode);
        var runJson = await runResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("succeeded", runJson.GetProperty("status").GetString());
        Assert.AreEqual("ws-1", runJson.GetProperty("workspace_id").GetString());
        Assert.AreEqual("build", runJson.GetProperty("workflow").GetString());

        // Content privacy: the run record never exposes message/output text.
        var rawRun = runJson.GetRawText();
        Assert.IsFalse(rawRun.Contains("recorded output"));
    }
}
