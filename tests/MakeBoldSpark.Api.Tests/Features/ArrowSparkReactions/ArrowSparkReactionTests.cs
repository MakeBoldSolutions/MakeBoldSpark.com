using System.Net;
using System.Text;
using System.Text.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MakeBoldSpark.Api.Tests.Features.ArrowSparkReactions;

[TestClass]
public class ArrowSparkReactionTests
{
    private const string Route = "/api/public/arrowspark/reactions";
    private const string Origin = "https://arrow.makeboldspark.com";
    private const string ValidGame = """{"reactionType":"game","readStoryFirst":"no","finished":"finished","satisfaction":4,"playAnother":"yes","comment":"Great 🦊","attempt":{"puzzleId":"reference_knot","puzzleVersion":"g1-0123456789ab","mistakes":2,"openMoveAssists":2,"score":103,"elapsedSeconds":1260}}""";

    private readonly MakeBoldSparkWebApplicationFactory baseFactory = new();
    private string storage = string.Empty;
    private WebApplicationFactory<Program> factory = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        await baseFactory.InitializeAsync();
        storage = Path.Combine(Path.GetTempPath(), "arrow-reactions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(storage);
        factory = Configure(new Dictionary<string, string?>());
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await baseFactory.DisposeAsync();
        if (Directory.Exists(storage)) Directory.Delete(storage, recursive: true);
    }

    private WebApplicationFactory<Program> Configure(Dictionary<string, string?> extra) =>
        baseFactory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, config) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ArrowSparkReactions:StoragePath"] = storage,
                ["ArrowSparkReactions:RateLimitPerMinute"] = "1000",
            };
            foreach (var pair in extra) values[pair.Key] = pair.Value;
            config.AddInMemoryCollection(values);
        }));

    private static ByteArrayContent Json(string body, string mediaType = "application/json")
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        content.Headers.TryAddWithoutValidation("Content-Type", mediaType);
        return content;
    }

    private Task<HttpResponseMessage> Post(string body, string mediaType = "application/json") =>
        factory.CreateClient().PostAsync(Route, Json(body, mediaType));

    private string[] Files() => Directory.GetFiles(storage);

    [TestMethod]
    public async Task Valid_game_is_202_with_empty_body_and_one_canonical_file()
    {
        var response = await Post(ValidGame);
        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        Assert.AreEqual(string.Empty, await response.Content.ReadAsStringAsync());
        Assert.IsNull(response.Headers.Location);
        Assert.IsTrue(response.Headers.CacheControl!.NoStore);
        Assert.IsFalse(response.Headers.Contains("Set-Cookie"));

        var file = Files().Single();
        Assert.EndsWith(".json", file);
        using var record = JsonDocument.Parse(File.ReadAllText(file));
        var root = record.RootElement;
        CollectionAssert.AreEqual(new[] { "recordId", "schemaVersion", "receivedUtc", "reactionType", "payload" },
            root.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.AreEqual(Path.GetFileNameWithoutExtension(file), root.GetProperty("recordId").GetString());
        Assert.AreEqual(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(root.GetProperty("receivedUtc").GetString()!, @"^\d{4}-\d\d-\d\dT\d\d:\d\dZ$"));
        // Same values and contract order; the writer may \u-escape non-BMP characters such as the emoji.
        using var sent = JsonDocument.Parse(ValidGame);
        Assert.IsTrue(JsonElement.DeepEquals(sent.RootElement, root.GetProperty("payload")));
        CollectionAssert.AreEqual(sent.RootElement.EnumerateObject().Select(p => p.Name).ToArray(),
            root.GetProperty("payload").EnumerateObject().Select(p => p.Name).ToArray());
        Assert.AreEqual(0, Directory.GetFiles(storage, ".tmp-*").Length);
    }

    [TestMethod]
    [DataRow("""{"reactionType":"story","madeSense":"partly","changedView":"moreInterested","wouldUse":"maybe"}""")]
    [DataRow("""{"reactionType":"story"}""")]
    [DataRow("""{"reactionType":"game","satisfaction":1}""")]
    [DataRow("""{"reactionType":"game","satisfaction":5}""")]
    [DataRow("""{"reactionType":"game","comment":"x"}""")]
    [DataRow("""{"reactionType":"game","attempt":{"puzzleId":"a","puzzleVersion":"b","mistakes":0,"openMoveAssists":10000,"score":10000,"elapsedSeconds":86400}}""")]
    public async Task Valid_bodies_are_accepted(string body) =>
        Assert.AreEqual(HttpStatusCode.Accepted, (await Post(body)).StatusCode);

    [TestMethod]
    public async Task Comment_boundaries_count_utf16_units()
    {
        var thousand = new string('a', 998) + "🦊"; // 998 + 2 units
        Assert.AreEqual(HttpStatusCode.Accepted, (await Post($$"""{"reactionType":"game","comment":"{{thousand}}"}""")).StatusCode);
        var over = new string('a', 999) + "🦊"; // 1,001 units
        Assert.AreEqual(HttpStatusCode.BadRequest, (await Post($$"""{"reactionType":"game","comment":"{{over}}"}""")).StatusCode);
    }

    [TestMethod]
    [DataRow("""{"reactionType":"game","visitorId":"x"}""", "body")]
    [DataRow("""{"reactionType":"game","satisfaction":6}""", "satisfaction")]
    [DataRow("""{"reactionType":"game","satisfaction":0}""", "satisfaction")]
    [DataRow("""{"reactionType":"game","satisfaction":"4"}""", "satisfaction")]
    [DataRow("""{"reactionType":"game","satisfaction":4.5}""", "satisfaction")]
    [DataRow("""{"reactionType":"game","satisfaction":4.0}""", "satisfaction")]
    [DataRow("""{"reactionType":"game","satisfaction":4e0}""", "satisfaction")]
    [DataRow("""{"reactionType":"story","attempt":{"puzzleId":"a"}}""", "attempt")]
    [DataRow("""{"reactionType":"game","attempt":{"puzzleId":"reference_knot"}}""", "attempt.puzzleVersion")]
    [DataRow("""{"reactionType":"game","attempt":{"puzzleId":"a","puzzleVersion":"b","mistakes":0,"openMoveAssists":0,"score":0,"elapsedSeconds":0,"extra":1}}""", "attempt")]
    [DataRow("""{"reactionType":"game","attempt":{"puzzleId":"Reference Knot","puzzleVersion":"b","mistakes":0,"openMoveAssists":0,"score":0,"elapsedSeconds":0}}""", "attempt.puzzleId")]
    [DataRow("""{"reactionType":"game","attempt":{"puzzleId":"a","puzzleVersion":"b","mistakes":10001,"openMoveAssists":0,"score":0,"elapsedSeconds":0}}""", "attempt.mistakes")]
    [DataRow("""{"reactionType":"game","attempt":{"puzzleId":"a","puzzleVersion":"b","mistakes":0,"openMoveAssists":0,"score":0,"elapsedSeconds":86401}}""", "attempt.elapsedSeconds")]
    [DataRow("""{"reactionType":"poll"}""", "reactionType")]
    [DataRow("""{"satisfaction":3}""", "reactionType")]
    [DataRow("""{"reactionType":"game","finished":"Finished"}""", "finished")]
    [DataRow("""{"reactionType":"game","comment":""}""", "comment")]
    [DataRow("""{"reactionType":"game","comment":null}""", "comment")]
    [DataRow("{\"reactionType\":\"game\"", "body")]
    [DataRow("[]", "body")]
    [DataRow("\"x\"", "body")]
    [DataRow("null", "body")]
    [DataRow("""{"reactionType":"game","reactionType":"story"}""", "body")]
    public async Task Invalid_bodies_are_400_naming_only_the_property_and_store_nothing(string body, string property)
    {
        var response = await Post(body);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual(property, problem.RootElement.GetProperty("property").GetString());
        Assert.AreEqual(0, Files().Length);
    }

    [TestMethod]
    public async Task Problem_response_never_echoes_visitor_text()
    {
        var response = await Post("""{"reactionType":"game","leakyPropertyName":"leakyValue"}""");
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("leaky", text);
    }

    [TestMethod]
    public async Task Size_and_media_type_limits()
    {
        var pad = new string('a', 4096 - """{"reactionType":"game","comment":""}""".Length);
        var exact = $$"""{"reactionType":"game","comment":"{{pad}}"}""";
        Assert.AreEqual(4096, Encoding.UTF8.GetByteCount(exact));
        // comment length (>1000) fails validation, but size passes: expect 400 not 413.
        Assert.AreEqual(HttpStatusCode.BadRequest, (await Post(exact)).StatusCode);
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, (await Post(exact + " ")).StatusCode);
        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, (await Post(ValidGame, "text/plain")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Accepted, (await Post(ValidGame, "application/json; charset=utf-8")).StatusCode);
    }

    [TestMethod]
    public async Task Markup_in_comment_is_stored_as_inert_json_string()
    {
        var body = JsonSerializer.Serialize(new { reactionType = "story", comment = "<b>\"hi\"</b>\nline2 }{" });
        Assert.AreEqual(HttpStatusCode.Accepted, (await Post(body)).StatusCode);
        using var record = JsonDocument.Parse(File.ReadAllText(Files().Single()));
        Assert.AreEqual("<b>\"hi\"</b>\nline2 }{", record.RootElement.GetProperty("payload").GetProperty("comment").GetString());
    }

    [TestMethod]
    public async Task Concurrent_requests_each_store_exactly_one_complete_file()
    {
        var client = factory.CreateClient();
        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => client.PostAsync(Route, Json(ValidGame))));
        Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.Accepted));
        var files = Files();
        Assert.AreEqual(20, files.Length);
        Assert.IsTrue(files.All(f => f.EndsWith(".json")));
        foreach (var f in files) JsonDocument.Parse(File.ReadAllText(f)).Dispose();
    }

    [TestMethod]
    public async Task Missing_storage_directory_is_5xx_never_202()
    {
        Directory.Delete(storage, recursive: true);
        var response = await Post(ValidGame);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(storage, text);
        Directory.CreateDirectory(storage);
    }

    [TestMethod]
    public async Task File_count_ceiling_returns_503()
    {
        using var capped = Configure(new() { ["ArrowSparkReactions:MaxFiles"] = "2" });
        var client = capped.CreateClient();
        Assert.AreEqual(HttpStatusCode.Accepted, (await client.PostAsync(Route, Json(ValidGame))).StatusCode);
        Assert.AreEqual(HttpStatusCode.Accepted, (await client.PostAsync(Route, Json(ValidGame))).StatusCode);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, (await client.PostAsync(Route, Json(ValidGame))).StatusCode);
        Assert.AreEqual(2, Files().Length);
    }

    [TestMethod]
    public async Task Rate_limit_returns_429_and_stores_nothing_more()
    {
        using var limited = Configure(new() { ["ArrowSparkReactions:RateLimitPerMinute"] = "2" });
        var client = limited.CreateClient();
        await client.PostAsync(Route, Json(ValidGame));
        await client.PostAsync(Route, Json(ValidGame));
        var third = await client.PostAsync(Route, Json(ValidGame));
        Assert.AreEqual(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.IsTrue(third.Headers.CacheControl!.NoStore);
        Assert.AreEqual(2, Files().Length);
    }

    [TestMethod]
    public async Task Disabled_switch_returns_410_and_stores_nothing()
    {
        using var disabled = Configure(new() { ["ArrowSparkReactions:Disabled"] = "true" });
        var response = await disabled.CreateClient().PostAsync(Route, Json(ValidGame));
        Assert.AreEqual(HttpStatusCode.Gone, response.StatusCode);
        Assert.AreEqual(string.Empty, await response.Content.ReadAsStringAsync());
        Assert.AreEqual(0, Files().Length);
    }

    [TestMethod]
    public async Task Cors_allows_only_the_showcase_origin()
    {
        var client = factory.CreateClient();
        var preflight = new HttpRequestMessage(HttpMethod.Options, Route);
        preflight.Headers.Add("Origin", Origin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");
        var pre = await client.SendAsync(preflight);
        Assert.AreEqual(HttpStatusCode.NoContent, pre.StatusCode);
        Assert.AreEqual(Origin, pre.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.IsFalse(pre.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.AreEqual("600", pre.Headers.GetValues("Access-Control-Max-Age").Single());

        var other = new HttpRequestMessage(HttpMethod.Post, Route) { Content = Json(ValidGame) };
        other.Headers.Add("Origin", "https://evil.example");
        Assert.IsFalse((await client.SendAsync(other)).Headers.Contains("Access-Control-Allow-Origin"));

        var allowed = new HttpRequestMessage(HttpMethod.Post, Route) { Content = Json(ValidGame) };
        allowed.Headers.Add("Origin", Origin);
        Assert.AreEqual(Origin, (await client.SendAsync(allowed)).Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [TestMethod]
    public async Task Other_methods_are_not_routed()
    {
        var response = await factory.CreateClient().GetAsync(Route);
        Assert.IsTrue(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);
    }

    [TestMethod]
    public async Task Stored_file_and_logs_carry_no_request_metadata()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Route) { Content = Json("""{"reactionType":"story","comment":"secret-comment-text"}""") };
        request.Headers.Add("Origin", Origin);
        request.Headers.Add("Referer", "https://referrer.example/page");
        request.Headers.UserAgent.ParseAdd("ArrowTestAgent/9.9");
        Assert.AreEqual(HttpStatusCode.Accepted, (await factory.CreateClient().SendAsync(request)).StatusCode);

        var file = File.ReadAllText(Files().Single());
        foreach (var banned in new[] { "arrow.makeboldspark", "referrer.example", "ArrowTestAgent", "127.0.0.1" })
            Assert.DoesNotContain(banned, file);
        Assert.IsFalse(baseFactory.CapturedLogs.Any(l => l.Contains("secret-comment-text") || l.Contains("ArrowTestAgent")));
        Assert.IsFalse(baseFactory.CapturedLogs.Any(l => l.Contains(Route)), "successful reactions must not be logged per request");
    }
}
