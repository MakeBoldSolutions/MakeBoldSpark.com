using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;

namespace MakeBoldSpark.Api.Tests.Features.AsyncDemo;

[TestClass]
public class AsyncDemoEndpointTests
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

    // ── Status ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetAsyncDemoStatus_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/async-demo/status/");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAsyncDemoStatus_ReturnsExpectedFields()
    {
        var response = await _client.GetAsync("/api/async-demo/status/");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.IsTrue(doc.RootElement.TryGetProperty("status", out _), "Missing 'status' field");
        Assert.IsTrue(doc.RootElement.TryGetProperty("buildDate", out _), "Missing 'buildDate' field");
        Assert.IsTrue(doc.RootElement.TryGetProperty("region", out _), "Missing 'region' field");
    }

    [TestMethod]
    public async Task GetAsyncDemoAppSettings_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/async-demo/status/appsettings");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAsyncDemoAppSettings_ReturnsExpectedFields()
    {
        var response = await _client.GetAsync("/api/async-demo/status/appsettings");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.IsTrue(doc.RootElement.TryGetProperty("testIds", out _), "Missing 'testIds'");
        Assert.IsTrue(doc.RootElement.TryGetProperty("testId", out _), "Missing 'testId'");
        Assert.IsTrue(doc.RootElement.TryGetProperty("testNames", out _), "Missing 'testNames'");
        Assert.IsTrue(doc.RootElement.TryGetProperty("testName", out _), "Missing 'testName'");
    }

    // ── Cancellation Patterns ───────────────────────────────────────────────

    [TestMethod]
    public async Task CancellationNoCancellation_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/async-demo/cancellation/no-cancellation?iterations=10");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task CancellationNoCancellation_ReturnsCancellableFalse()
    {
        var response = await _client.GetAsync("/api/async-demo/cancellation/no-cancellation?iterations=5");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.IsFalse(doc.RootElement.GetProperty("cancellable").GetBoolean());
        Assert.IsTrue(doc.RootElement.TryGetProperty("result", out _));
        Assert.IsTrue(doc.RootElement.TryGetProperty("iterations", out _));
        Assert.IsTrue(doc.RootElement.TryGetProperty("elapsedMilliseconds", out _));
    }

    [TestMethod]
    public async Task CancellationWithToken_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/async-demo/cancellation/with-token?iterations=5");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task CancellationWithToken_ReturnsCancellableTrue()
    {
        var response = await _client.GetAsync("/api/async-demo/cancellation/with-token?iterations=5");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.IsTrue(doc.RootElement.GetProperty("cancellable").GetBoolean());
    }

    [TestMethod]
    public async Task CancellationWithTimeout_ReturnsOkForShortWork()
    {
        var response = await _client.GetAsync("/api/async-demo/cancellation/with-timeout?iterations=5&timeoutSeconds=30");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task CancellationWithCleanup_ReturnsCleanupRan()
    {
        var response = await _client.GetAsync("/api/async-demo/cancellation/with-cleanup?iterations=5");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.IsTrue(doc.RootElement.GetProperty("cleanupRan").GetBoolean());
    }

    // ── Concurrency Patterns ────────────────────────────────────────────────

    [TestMethod]
    public async Task ConcurrencySequential_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/async-demo/concurrency/sequential?operationCount=3&iterationsPerOperation=5");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ConcurrencySequential_ReturnsExpectedMode()
    {
        var response = await _client.GetAsync("/api/async-demo/concurrency/sequential?operationCount=2&iterationsPerOperation=3");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.AreEqual("Sequential", doc.RootElement.GetProperty("executionMode").GetString());
        Assert.AreEqual(2, doc.RootElement.GetProperty("totalOperations").GetInt32());
        Assert.IsTrue(doc.RootElement.TryGetProperty("results", out var results));
        Assert.AreEqual(2, results.GetArrayLength());
    }

    [TestMethod]
    public async Task ConcurrencyParallel_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/async-demo/concurrency/parallel?operationCount=3&iterationsPerOperation=5");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ConcurrencyParallel_ReturnsSpeedupFactor()
    {
        var response = await _client.GetAsync("/api/async-demo/concurrency/parallel?operationCount=3&iterationsPerOperation=5");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.AreEqual("Parallel", doc.RootElement.GetProperty("executionMode").GetString());
        Assert.IsTrue(doc.RootElement.TryGetProperty("speedupFactor", out _));
    }

    [TestMethod]
    public async Task ConcurrencyThrottled_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/async-demo/concurrency/throttled?operationCount=4&maxConcurrency=2&iterationsPerOperation=3");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ConcurrencyComparison_ReturnsAllThreeModes()
    {
        var response = await _client.GetAsync("/api/async-demo/concurrency/comparison?operationCount=2&maxConcurrency=2&iterationsPerOperation=3");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.IsTrue(doc.RootElement.TryGetProperty("sequential", out _));
        Assert.IsTrue(doc.RootElement.TryGetProperty("parallel", out _));
        Assert.IsTrue(doc.RootElement.TryGetProperty("throttled", out _));
    }

    // ── Remote Mock ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task RemoteMockResults_CompletesSuccessfully()
    {
        var payload = new { loopCount = 3, maxTimeMS = 5000 };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/async-demo/remote/results", content);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.AreEqual("Task Complete", doc.RootElement.GetProperty("message").GetString());
    }

    [TestMethod]
    public async Task RemoteMockResults_TimesOutWhenBudgetTooSmall()
    {
        // 50 iterations × ~10ms each = ~500ms; maxTimeMS = 50 should time out
        var payload = new { loopCount = 50, maxTimeMS = 50 };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/async-demo/remote/results", content);
        Assert.AreEqual(HttpStatusCode.RequestTimeout, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.AreEqual("Time Out Occurred", doc.RootElement.GetProperty("message").GetString());
    }
}
