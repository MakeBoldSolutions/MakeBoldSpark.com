using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MakeBoldSpark.Api.Features.FamilyMemories;
using MakeBoldSpark.Api.Infrastructure.Data;
using MakeBoldSpark.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;


namespace MakeBoldSpark.Api.Tests.Features.FamilyMemories;

[TestClass]
public class FamilyMemoryTests
{
    private readonly MakeBoldSparkWebApplicationFactory factory = new();
    private const string Public = "/api/public/family-memories";
    private const string Admin = "/api/admin/makeboldspark/family-memories";
    [TestInitialize]
    public Task InitializeAsync() => factory.InitializeAsync();
    [TestCleanup]
    public Task DisposeAsync() => factory.DisposeAsync();
    private static FamilyMemoryWrite Fixture(int order = 0, bool published = false) => new()
    {
        PersonName = " Fictional Zoë 🦊 ", PersonPronouns = "they/them",
        RelationshipToJared = "Fictional family friend — café",
        Body = "  Fictional first paragraph.\r\n\r\n第二段 — [**Fictional Name**](https://example.com/a?q=1&b=2).\n\n<em>Literal text</em>  ",
        SortOrder = order, IsPublished = published,
    };
    private async Task<FamilyMemory> Create(FamilyMemoryWrite input)
    {
        using var client = factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync(Admin, input);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<FamilyMemory>())!;
        Assert.AreEqual($"{Admin}/{result.Id}", response.Headers.Location!.ToString());
        return result;
    }

    [TestMethod]
    public async Task Runtime_OpenApi_describes_public_projection_and_management_limits()
    {
        using var client = factory.CreateClient();
        var api = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var paths = api.GetProperty("paths");
        Assert.IsTrue(paths.GetProperty(Public).GetProperty("get").GetProperty("responses").TryGetProperty("500", out _));
        Assert.IsTrue(paths.GetProperty(Admin).TryGetProperty("post", out _));
        Assert.IsTrue(paths.GetProperty(Admin + "/{id}").TryGetProperty("put", out _));
        var schemas = api.GetProperty("components").GetProperty("schemas");
        CollectionAssert.AreEquivalent(new[] { "personName", "personPronouns", "relationshipToJared", "body" },
            schemas.GetProperty("PublishedMemory").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToArray());
        Assert.AreEqual(100000, schemas.GetProperty("FamilyMemoryWrite").GetProperty("properties").GetProperty("body").GetProperty("maxLength").GetInt32());
    }

    [TestMethod]
    public async Task Empty_is_200_with_exact_envelope_and_no_secrets()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        var response = await client.GetAsync(Public);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.IsTrue(response.Headers.CacheControl!.NoStore);
        Assert.AreEqual("{\"success\":true,\"data\":{\"memories\":[]}}", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Lifecycle_preserves_content_and_only_exposes_published_fields_in_editorial_order()
    {
        var input = Fixture(20, true);
        var later = await Create(input);
        var first = await Create(Fixture(10, true) with { PersonPronouns = "", PersonName = "Fictional first" });
        var tie = await Create(Fixture(10, true) with { PersonName = "Fictional tie" });
        var draft = await Create(Fixture(0));
        using var client = factory.CreateClient();
        using var admin = factory.CreateAdminClient();
        var response = await client.GetFromJsonAsync<JsonElement>(Public);
        CollectionAssert.AreEqual(new[] { "success", "data" }, response.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.IsTrue(response.GetProperty("success").GetBoolean());
        CollectionAssert.AreEqual(new[] { "memories" }, response.GetProperty("data").EnumerateObject().Select(p => p.Name).ToArray());
        var memories = response.GetProperty("data").GetProperty("memories").EnumerateArray().ToArray();
        Assert.AreEqual(3, memories.Length);
        CollectionAssert.AreEqual(new[] { first.PersonName, tie.PersonName, later.PersonName }, memories.Select(m => m.GetProperty("personName").GetString()).ToArray());
        Assert.AreEqual("", memories[0].GetProperty("personPronouns").GetString());
        foreach (var memory in memories)
        {
            CollectionAssert.AreEqual(new[] { "personName", "personPronouns", "relationshipToJared", "body" }, memory.EnumerateObject().Select(p => p.Name).ToArray());
            foreach (var p in memory.EnumerateObject()) Assert.AreEqual(JsonValueKind.String, p.Value.ValueKind);
            Assert.AreEqual(input.Body, memory.GetProperty("body").GetString());
        }
        // A separate context proves persisted values, rather than merely echoed request data.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>();
            var persisted = await db.FamilyMemories.AsNoTracking().SingleAsync(m => m.Id == later.Id);
            Assert.AreEqual(input.Body, persisted.Body);
            Assert.AreEqual(input.PersonName, persisted.PersonName);
            Assert.AreEqual(input.RelationshipToJared, persisted.RelationshipToJared);
        }
        var all = (await admin.GetFromJsonAsync<List<FamilyMemory>>(Admin))!;
        CollectionAssert.AreEqual(new[] { draft.Id, first.Id, tie.Id, later.Id }, all.Select(m => m.Id).ToArray());
        var update = await admin.PutAsJsonAsync($"{Admin}/{later.Id}", input with { SortOrder = 1, Body = "Updated fictional memory.\n\nStill text." });
        Assert.AreEqual(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<FamilyMemory>())!;
        Assert.AreEqual(later.CreatedDate, updated.CreatedDate);
        Assert.IsTrue(updated.UpdatedDate >= later.UpdatedDate);
        var published = (await client.GetFromJsonAsync<PublishedMemoriesResponse>(Public))!;
        Assert.AreEqual(updated.Body, published.Data.Memories[0].Body);
        Assert.AreEqual(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"{Admin}/{later.Id}", input with { IsPublished = false })).StatusCode);
        Assert.AreEqual(2, (await client.GetFromJsonAsync<PublishedMemoriesResponse>(Public))!.Data.Memories.Count);
        Assert.AreEqual(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"{Admin}/{draft.Id}", Fixture(0, true))).StatusCode);
        Assert.AreEqual(3, (await client.GetFromJsonAsync<PublishedMemoriesResponse>(Public))!.Data.Memories.Count);
        Assert.AreEqual(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Admin}/{first.Id}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await admin.GetAsync($"{Admin}/{first.Id}")).StatusCode);
        Assert.AreEqual(2, (await client.GetFromJsonAsync<PublishedMemoriesResponse>(Public))!.Data.Memories.Count);
    }

    [TestMethod]
    public async Task Entire_collection_and_maximum_length_text_are_returned()
    {
        var longText = string.Concat(Enumerable.Repeat("Fictional.\n\n", 8333)) + "xxxx";
        Assert.AreEqual(100000, longText.Length);
        await Create(Fixture(0, true) with { Body = longText, PersonName = new string('N',200), PersonPronouns = new string('p',100), RelationshipToJared = new string('r',300) });
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>();
            for (var i = 1; i <= 105; i++) db.FamilyMemories.Add(new FamilyMemory
            { PersonName = $"Fictional {i}", PersonPronouns = "", RelationshipToJared = "Fictional friend", Body = "Fictional.", IsPublished = true, SortOrder = i });
            await db.SaveChangesAsync();
        }
        using var client = factory.CreateClient();
        var result = (await client.GetFromJsonAsync<PublishedMemoriesResponse>(Public))!;
        Assert.AreEqual(106, result.Data.Memories.Count);
        Assert.AreEqual(longText, result.Data.Memories[0].Body);
    }

    public static IEnumerable<object[]> InvalidInputs()
    {
        foreach (var field in new[] { "personName", "personPronouns", "relationshipToJared", "body" })
        {
            yield return [field, "null"];
            yield return [field, "123"];
            yield return [field, "MISSING"];
        }
        foreach (var field in new[] { "personName", "relationshipToJared", "body" }) yield return [field, "\"  \\n\""];
        foreach (var (field, size) in new[] { ("personName",201), ("personPronouns",101), ("relationshipToJared",301), ("body",100001) })
            yield return [field, JsonSerializer.Serialize(new string('x', size))];
        yield return ["sortOrder", "-1"];
        yield return ["sortOrder", "1.5"];
        yield return ["sortOrder", "2147483648"];
        yield return ["isPublished", "\"true\""];
    }

    [TestMethod]
    [DynamicData(nameof(InvalidInputs))]
    public async Task Invalid_input_is_400_and_does_not_change_persistence(string field, string raw)
    {
        var original = await Create(Fixture());
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(Fixture(), new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
        if (raw == "MISSING") values.Remove(field);
        else values[field] = JsonSerializer.Deserialize<JsonElement>(raw);
        using var admin = factory.CreateAdminClient();
        var json = JsonSerializer.Serialize(values);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await admin.PostAsync(Admin, new StringContent(json, Encoding.UTF8, "application/json"))).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await admin.PutAsync($"{Admin}/{original.Id}", new StringContent(json, Encoding.UTF8, "application/json"))).StatusCode);
        var all = (await admin.GetFromJsonAsync<List<FamilyMemory>>(Admin))!;
        Assert.HasCount(1, all);
        Assert.AreEqual(original.Body, all[0].Body);
        Assert.AreEqual(original.UpdatedDate, all[0].UpdatedDate);
    }

    [TestMethod]
    [DataRow("GET", "")]
    [DataRow("GET", "/1")]
    [DataRow("POST", "")]
    [DataRow("PUT", "/1")]
    [DataRow("DELETE", "/1")]
    public async Task Every_management_operation_requires_admin(string method, string suffix)
    {
        using var anonymous = factory.CreateClient();
        using var publisher = factory.CreatePublisherClient();
        using var nonAdmin = factory.CreateClient();
        nonAdmin.DefaultRequestHeaders.Add("X-Test-Claims", "Reader");
        foreach (var (client, expected) in new[] { (anonymous,HttpStatusCode.Unauthorized),(publisher,HttpStatusCode.Forbidden),(nonAdmin,HttpStatusCode.Forbidden) })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), Admin + suffix) { Content = JsonContent.Create(Fixture()) };
            Assert.AreEqual(expected, (await client.SendAsync(request)).StatusCode);
        }
    }

    [TestMethod]
    public async Task Missing_records_and_malformed_json_use_error_statuses()
    {
        using var admin = factory.CreateAdminClient();
        Assert.AreEqual(HttpStatusCode.NotFound, (await admin.GetAsync(Admin + "/9999")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync(Admin + "/9999", Fixture())).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await admin.DeleteAsync(Admin + "/9999")).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await admin.PostAsync(Admin, new StringContent("{", Encoding.UTF8, "application/json"))).StatusCode);
    }

    [TestMethod]
    public async Task Database_failure_is_500_with_generic_problem_and_Cors()
    {
        using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>().Database.ExecuteSqlRawAsync("DROP TABLE FamilyMemories");
        client.DefaultRequestHeaders.Add("Origin", "https://texecon.com");
        var response = await client.GetAsync(Public);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.AreEqual("https://texecon.com", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.IsFalse((await response.Content.ReadAsStringAsync()).Contains("SQLite"));
        using var admin = factory.CreateAdminClient();
        Assert.AreEqual(HttpStatusCode.InternalServerError, (await admin.PostAsJsonAsync(Admin, Fixture())).StatusCode);
    }

    [TestMethod]
    [DataRow("https://texecon.com", true)]
    [DataRow("http://localhost:5199", true)]
    [DataRow("http://localhost:5173", false)]
    [DataRow("https://unconfigured.example", false)]
    public async Task Browser_get_and_preflight_only_allow_configured_origins(string origin, bool allowed)
    {
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string,string?> { ["FamilyMemories:DevelopmentOrigins:0"] = "http://localhost:5199" })));
        using var client = configured.CreateClient();
        using var get = new HttpRequestMessage(HttpMethod.Get, Public);
        get.Headers.Add("Origin", origin);
        get.Headers.Accept.ParseAdd("application/json");
        var response = await client.SendAsync(get);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        using var preflight = new HttpRequestMessage(HttpMethod.Options, Public);
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", "GET");
        preflight.Headers.Add("Access-Control-Request-Headers", "accept");
        var options = await client.SendAsync(preflight);
        Assert.AreEqual(HttpStatusCode.NoContent, options.StatusCode);
        Assert.AreEqual(allowed, options.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.IsFalse(options.Headers.Contains("Access-Control-Allow-Credentials"));
        if (allowed)
        {
            Assert.AreEqual(origin, options.Headers.GetValues("Access-Control-Allow-Origin").Single());
            StringAssert.Contains(options.Headers.GetValues("Access-Control-Allow-Methods").Single(), "GET");
        }
    }
}

