using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MakeBoldSpark.Api.Features.Bold;
using Microsoft.Extensions.Options;
using WebSpark.HttpClientUtility.RequestResult;

namespace MakeBoldSpark.Api.Features.Bold.Providers;

/// <summary>
/// Thin client for the OpenAI Responses API, built on WebSpark.HttpClientUtility for HTTP plumbing
/// (spec.md Intent: provider integration style). The named HttpClient "bold-openai" is registered
/// with the configured base URL, API key, and per-attempt timeout in Program.cs.
/// </summary>
public sealed class OpenAiProviderClient : IProviderClient
{
    public const string HttpClientName = "bold-openai";
    private const string CompletionsPath = "/v1/responses";
    private const string ModelsPath = "/v1/models";

    private readonly HttpRequestResultService _http;

    public OpenAiProviderClient(
        IHttpClientFactory httpClientFactory,
        IOptions<BoldOptions> options,
        IConfiguration configuration,
        ILogger<HttpRequestResultService> httpLogger)
    {
        var boldOptions = options.Value;
        var client = httpClientFactory.CreateClient(HttpClientName);
        if (client.BaseAddress is null)
            client.BaseAddress = new Uri(boldOptions.OpenAi.BaseUrl);
        if (!string.IsNullOrWhiteSpace(boldOptions.OpenAi.ApiKey) && client.DefaultRequestHeaders.Authorization is null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", boldOptions.OpenAi.ApiKey);

        _http = new HttpRequestResultService(httpLogger, configuration, client);
    }

    public string Provider => "openai";

    public async Task<ProviderCompletionResult> CompleteAsync(ProviderCompletionRequest request, CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = request.Model,
            input = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            max_output_tokens = request.MaxOutputTokens,
            temperature = request.Temperature,
        };

        var result = new HttpRequestResult<OpenAiResponsesEnvelope>
        {
            RequestPath = CompletionsPath,
            RequestMethod = HttpMethod.Post,
            RequestBody = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };

        var response = await _http.HttpSendRequestResultAsync(result, Guid.NewGuid().ToString("n"), nameof(OpenAiProviderClient), 0, cancellationToken);

        if (!response.IsSuccessStatusCode || response.ResponseResults is null)
            throw ProviderErrorClassifier.Classify("OpenAI", response.StatusCode, response.ResponseResults?.Error?.Message ?? string.Join("; ", response.ErrorList));

        var envelope = response.ResponseResults;
        var text = string.Concat(
            (envelope.Output ?? [])
                .SelectMany(o => o.Content ?? [])
                .Where(c => c.Type is "output_text" or "text")
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
        var response = await _http.HttpSendRequestResultAsync(result, Guid.NewGuid().ToString("n"), $"{nameof(OpenAiProviderClient)}.Health", 0, cancellationToken);
        var latencyMs = (int)(DateTimeOffset.UtcNow - started).TotalMilliseconds;

        return response.IsSuccessStatusCode
            ? new ProviderHealth(true, latencyMs, null)
            : new ProviderHealth(false, latencyMs, $"OpenAI reachability check returned {(int)response.StatusCode}.");
    }
}
