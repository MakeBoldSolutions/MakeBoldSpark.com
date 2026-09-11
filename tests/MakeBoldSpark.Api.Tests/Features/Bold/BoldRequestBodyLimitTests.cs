using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MakeBoldSpark.Api.Features.Bold.Completions;
using MakeBoldSpark.Api.Tests.Infrastructure;

namespace MakeBoldSpark.Api.Tests.Features.Bold;

[TestClass]
public class BoldRequestBodyLimitTests
{
    [TestMethod]
    public async Task Reader_StopsAtLimitPlusOneWithoutBufferingTheWholeStream()
    {
        using var stream = new MemoryStream(new byte[1024 * 1024]);
        Assert.IsNull(await BoundedRequestBody.ReadAsync(stream, 1024, CancellationToken.None));
        Assert.AreEqual(1025L, stream.Position);
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, false)]
    [DataRow(true, true)]
    [DataRow(false, true)]
    public async Task Endpoint_EnforcesByteLimitWithAndWithoutContentLength(bool knownLength, bool oversize)
    {
        await using var factory = new MakeBoldSparkWebApplicationFactory();
        await factory.InitializeAsync();
        var calls = 0;
        factory.OpenAiHandler = _ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"output\":[],\"usage\":{}}", Encoding.UTF8, "application/json") };
        };
        var (client, _, _) = await factory.CreateBoldClientAsync();
        const int limit = 400 * 1024;
        var json = "{\"model_role\":\"router\",\"messages\":[{\"role\":\"user\",\"content\":\"Fictional café 🌻\"}]}";
        var bytes = Encoding.UTF8.GetBytes(json + new string(' ', limit - Encoding.UTF8.GetByteCount(json) + (oversize ? 1 : 0)));
        using HttpContent content = knownLength ? new ByteArrayContent(bytes) : new UnknownLengthContent(bytes);
        content.Headers.ContentType = new("application/json");
        var response = await client.PostAsync("/api/integrations/bold/v1/completions", content);
        Assert.AreEqual(oversize ? HttpStatusCode.BadRequest : HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(oversize ? 0 : 1, calls);
        if (oversize)
        {
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("request_too_large", result.GetProperty("error").GetProperty("code").GetString());
        }
    }

    private sealed class UnknownLengthContent(byte[] bytes) : HttpContent
    {
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => stream.WriteAsync(bytes).AsTask();
    }
}
