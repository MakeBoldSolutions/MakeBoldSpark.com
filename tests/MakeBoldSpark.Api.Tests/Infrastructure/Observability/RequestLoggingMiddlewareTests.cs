using System.Collections.Concurrent;
using System.Net;
using MakeBoldSpark.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MakeBoldSpark.Api.Tests.Infrastructure.Observability;

/// <summary>
/// Verifies FR-009: all 9 required structured log fields are captured
/// by RequestLoggingMiddleware on every request.
/// Fields: Method, RequestPath, StatusCode, DurationMs,
///         CorrelationId, UserId, FeatureName, OperationName, Success
/// </summary>
[TestClass]
public class RequestLoggingMiddlewareTests
{
    private static LoggingTestFactory _factory = null!;
    private HttpClient _client = null!;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _factory = new LoggingTestFactory();
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

    [TestMethod]
    public async Task RequestLogging_CapturesMethod()
    {
        _factory.Capture.Messages.Clear();
        await _client.GetAsync("/api/health");
        Assert.IsTrue(_factory.Capture.Messages.Any(m => m.Contains("HTTP GET")));
    }

    [TestMethod]
    public async Task RequestLogging_CapturesRequestPath()
    {
        _factory.Capture.Messages.Clear();
        await _client.GetAsync("/api/health");
        Assert.IsTrue(_factory.Capture.Messages.Any(m => m.Contains("/api/health")));
    }

    [TestMethod]
    public async Task RequestLogging_CapturesStatusCode()
    {
        _factory.Capture.Messages.Clear();
        await _client.GetAsync("/api/health");
        Assert.IsTrue(_factory.Capture.Messages.Any(m => m.Contains("responded 200")));
    }

    [TestMethod]
    public async Task RequestLogging_CapturesDurationMs()
    {
        _factory.Capture.Messages.Clear();
        await _client.GetAsync("/api/health");
        Assert.IsTrue(_factory.Capture.Messages.Any(m => m.Contains("ms |")));
    }

    [TestMethod]
    public async Task RequestLogging_CapturesCorrelationId()
    {
        _factory.Capture.Messages.Clear();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        req.Headers.Add("X-Correlation-ID", "test-corr-123");
        await _client.SendAsync(req);
        Assert.IsTrue(_factory.Capture.Messages.Any(m => m.Contains("CorrelationId=test-corr-123")));
    }

    [TestMethod]
    public async Task RequestLogging_UserId_IsAnonymousForUnauthenticated()
    {
        _factory.Capture.Messages.Clear();
        await _client.GetAsync("/api/health");
        Assert.IsTrue(_factory.Capture.Messages.Any(m => m.Contains("UserId=anonymous")));
    }

    [TestMethod]
    public async Task RequestLogging_CapturesFeatureName()
    {
        _factory.Capture.Messages.Clear();
        await _client.GetAsync("/api/health");
        Assert.IsTrue(_factory.Capture.Messages.Any(m => m.Contains("Feature=")));
    }

    [TestMethod]
    public async Task RequestLogging_CapturesOperationName()
    {
        _factory.Capture.Messages.Clear();
        await _client.GetAsync("/api/health");
        Assert.IsTrue(_factory.Capture.Messages.Any(m => m.Contains("Operation=")));
    }

    [TestMethod]
    public async Task RequestLogging_CapturesSuccessTrue_For2xx()
    {
        _factory.Capture.Messages.Clear();
        await _client.GetAsync("/api/health");
        Assert.IsTrue(_factory.Capture.Messages.Any(m => m.Contains("Success=True")));
    }

    [TestMethod]
    public async Task RequestLogging_CapturesSuccessFalse_For4xx()
    {
        _factory.Capture.Messages.Clear();
        await _client.GetAsync("/api/public/content/articles/nonexistent-article");
        Assert.IsTrue(_factory.Capture.Messages.Any(m => m.Contains("Success=False")));
    }

    [TestMethod]
    public async Task RequestLogging_ResponseHeader_ContainsCorrelationId()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.IsTrue(response.Headers.Contains("X-Correlation-ID"),
            "Response should echo X-Correlation-ID header");
    }
}

/// <summary>
/// Test factory that extends the shared MakeBoldSparkWebApplicationFactory and
/// adds a CaptureLoggerProvider so tests can assert on log message content.
/// Inherits SQLite shared-cache setup and TestAuthHandler from the base class.
/// </summary>
public class LoggingTestFactory : MakeBoldSparkWebApplicationFactory
{
    public CaptureLoggerProvider Capture { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
            services.AddSingleton<ILoggerProvider>(Capture));
    }
}

/// <summary>
/// Thread-safe ILoggerProvider that accumulates formatted log messages.
/// </summary>
public sealed class CaptureLoggerProvider : ILoggerProvider
{
    public ConcurrentBag<string> Messages { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CaptureLogger(Messages);

    public void Dispose() { }
}

internal sealed class CaptureLogger(ConcurrentBag<string> messages) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
            messages.Add(formatter(state, exception));
    }
}
