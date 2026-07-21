using System.Text;
using System.Text.Json;
using MakeBoldSpark.Api.Features.Bold;
using Microsoft.Extensions.Options;
using WebSpark.HttpClientUtility.RequestResult;

namespace MakeBoldSpark.Api.Features.Bold.Providers;

/// <summary>
/// Thin client for the Anthropic Messages API, built on WebSpark.HttpClientUtility for HTTP
/// plumbing (spec.md Intent: provider integration style). The named HttpClient "bold-anthropic" is
/// registered with the configured base URL, API key headers, and per-attempt timeout in Program.cs.
/// </summary>
public sealed class AnthropicProviderClient : IProviderClient
{
    public const string HttpClientName = "bold-anthropic";
    private const string CompletionsPath = "/v1/messages";
    private const string ModelsPath = "/v1/models";

    private readonly HttpRequestResultService _http;
    private readonly int _maxOutputTokensDefault;

    public AnthropicProviderClient(
        IHttpClientFactory httpClientFactory,
        IOptions<BoldOptions> options,
        IConfiguration configuration,
        ILogger<HttpRequestResultService> httpLogger)
    {
        var boldOptions = options.Value;
        var client = httpClientFactory.CreateClient(HttpClientName);
        if (client.BaseAddress is null)
            client.BaseAddress = new Uri(boldOptions.Anthropic.BaseUrl);
        if (!string.IsNullOrWhiteSpace(boldOptions.Anthropic.ApiKey) && !client.DefaultRequestHeaders.Contains("x-api-key"))
            client.DefaultRequestHeaders.Add("x-api-key", boldOptions.Anthropic.ApiKey);
        if (!client.DefaultRequestHeaders.Contains("anthropic-version"))
            client.DefaultRequestHeaders.Add("anthropic-version", boldOptions.Anthropic.ApiVersion);

        _http = new HttpRequestResultService(httpLogger, configuration, client);
        _maxOutputTokensDefault = boldOptions.RequestBounds.MaxOutputTokensCeiling;
    }

    public string Provider => "anthropic";

    public async Task<ProviderCompletionResult> CompleteAsync(ProviderCompletionRequest request, CancellationToken cancellationToken)
    {
        var systemMessage = request.Messages.FirstOrDefault(m => m.Role == "system")?.Content;
        var conversation = request.Messages
            .Where(m => m.Role != "system")
            .Select(m => new { role = m.Role, content = m.Content });

        var payload = new
        {
            model = request.Model,
            max_tokens = request.MaxOutputTokens ?? _maxOutputTokensDefault,
            temperature = request.Temperature,
            system = systemMessage,
            messages = conversation,
        };

        var result = new HttpRequestResult<AnthropicMessagesEnvelope>
        {
            RequestPath = CompletionsPath,
            RequestMethod = HttpMethod.Post,
            RequestBody = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };

        var response = await _http.HttpSendRequestResultAsync(result, Guid.NewGuid().ToString("n"), nameof(AnthropicProviderClient), 0, cancellationToken);

        if (!response.IsSuccessStatusCode || response.ResponseResults is null)
            throw ProviderErrorClassifier.Classify("Anthropic", response.StatusCode, response.ResponseResults?.Error?.Message ?? string.Join("; ", response.ErrorList));

        var envelope = response.ResponseResults;
        var text = string.Concat(
            (envelope.Content ?? [])
                .Where(c => c.Type == "text")
                .Select(c => c.Text ?? string.Empty));

        return new ProviderCompletionResult(
            text,
            envelope.Usage?.InputTokens ?? 0,
            envelope.Usage?.OutputTokens ?? 0,
            envelope.Id);
    }

    public async Task<ProviderHealth> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        var result = new HttpRequestResult<JsonElement> { RequestPath = ModelsPath, RequestMethod = HttpMethod.Get };
        var response = await _http.HttpSendRequestResultAsync(result, Guid.NewGuid().ToString("n"), $"{nameof(AnthropicProviderClient)}.Health", 0, cancellationToken);
        var latencyMs = (int)(DateTimeOffset.UtcNow - started).TotalMilliseconds;

        return response.IsSuccessStatusCode
            ? new ProviderHealth(true, latencyMs, null)
            : new ProviderHealth(false, latencyMs, $"Anthropic reachability check returned {(int)response.StatusCode}.");
    }
}
