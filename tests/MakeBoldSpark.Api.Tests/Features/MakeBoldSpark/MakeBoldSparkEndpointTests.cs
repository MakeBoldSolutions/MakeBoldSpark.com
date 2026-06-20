using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;
using MakeBoldSpark.Core.Data;

namespace MakeBoldSpark.Api.Tests.Features.MakeBoldSpark;

[TestClass]
public class MakeBoldSparkEndpointTests
{
    private static MakeBoldSparkWebApplicationFactory _factory = null!;
    private HttpClient _anonClient = null!;
    private HttpClient _adminClient = null!;

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
        _anonClient = _factory.CreateClient();
        _adminClient = _factory.CreateAdminClient();
    }

    // ── Public read endpoints (anonymous) ────────────────────────────────────

    [TestMethod]
    [DataRow("/api/public/makeboldspark/domains")]
    [DataRow("/api/public/makeboldspark/blogs")]
    [DataRow("/api/public/makeboldspark/authors")]
    [DataRow("/api/public/makeboldspark/posts")]
    [DataRow("/api/public/makeboldspark/categories")]
    [DataRow("/api/public/makeboldspark/menus")]
    [DataRow("/api/public/makeboldspark/keywords")]
    [DataRow("/api/public/makeboldspark/content-parts")]
    public async Task PublicMakeBoldSparkEndpoints_Anonymous_ReturnOk(string path)
    {
        var response = await _anonClient.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    [DataRow("/api/public/makeboldspark/domains")]
    [DataRow("/api/public/makeboldspark/blogs")]
    [DataRow("/api/public/makeboldspark/authors")]
    [DataRow("/api/public/makeboldspark/posts")]
    [DataRow("/api/public/makeboldspark/categories")]
    [DataRow("/api/public/makeboldspark/menus")]
    [DataRow("/api/public/makeboldspark/keywords")]
    [DataRow("/api/public/makeboldspark/content-parts")]
    public async Task PublicMakeBoldSparkListEndpoints_ReturnJsonArray(string path)
    {
        var response = await _anonClient.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.IsNotNull(body);
    }

    [TestMethod]
    public async Task PublicAuthorsList_DoesNotReturnPassword()
    {
        var createResponse = await _adminClient.PostAsJsonAsync("/api/admin/makeboldspark/authors", new Author
        {
            Email = "author-list-test@example.com",
            Password = "stored-password-hash",
            DisplayName = "Author List Test",
            Bio = "Visible biography",
            Avatar = "author.png"
        });
        createResponse.EnsureSuccessStatusCode();
        using var createdDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var authorId = createdDocument.RootElement.GetProperty("id").GetInt32();
        Assert.IsFalse(createdDocument.RootElement.TryGetProperty("password", out _));

        var response = await _anonClient.GetAsync("/api/public/makeboldspark/authors");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var author = document.RootElement.EnumerateArray()
            .First(item => item.GetProperty("email").GetString() == "author-list-test@example.com");

        Assert.IsFalse(author.TryGetProperty("password", out _));
        Assert.AreEqual("Author List Test", author.GetProperty("displayName").GetString());

        var detailResponse = await _anonClient.GetAsync($"/api/public/makeboldspark/authors/{authorId}");
        detailResponse.EnsureSuccessStatusCode();
        using var detailDocument = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.IsFalse(detailDocument.RootElement.TryGetProperty("password", out _));
        Assert.AreEqual("Author List Test", detailDocument.RootElement.GetProperty("displayName").GetString());
    }

    [TestMethod]
    public async Task AdminMailSettings_DoNotReturnUserPassword()
    {
        var blogResponse = await _adminClient.PostAsJsonAsync("/api/admin/makeboldspark/blogs", new Blog
        {
            Title = "Mail Settings Test Blog",
            Description = "Blog used by mail settings redaction test.",
            Theme = "default",
            ItemsPerPage = 10
        });
        blogResponse.EnsureSuccessStatusCode();
        using var blogDocument = JsonDocument.Parse(await blogResponse.Content.ReadAsStringAsync());
        var blogId = blogDocument.RootElement.GetProperty("id").GetInt32();

        var createResponse = await _adminClient.PostAsJsonAsync("/api/admin/makeboldspark/mail-settings", new MailSetting
        {
            Host = "smtp.example.com",
            Port = 587,
            UserEmail = "smtp-user@example.com",
            UserPassword = "smtp-secret",
            FromName = "MakeBoldSpark Tests",
            FromEmail = "from@example.com",
            ToName = "Test Recipient",
            Enabled = true,
            BlogId = blogId
        });
        createResponse.EnsureSuccessStatusCode();
        using var createdDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var mailSettingId = createdDocument.RootElement.GetProperty("id").GetInt32();
        Assert.IsFalse(createdDocument.RootElement.TryGetProperty("userPassword", out _));

        var detailResponse = await _adminClient.GetAsync($"/api/admin/makeboldspark/mail-settings/{mailSettingId}");
        detailResponse.EnsureSuccessStatusCode();
        using var detailDocument = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.IsFalse(detailDocument.RootElement.TryGetProperty("userPassword", out _));
        Assert.AreEqual("smtp-user@example.com", detailDocument.RootElement.GetProperty("userEmail").GetString());

        var listResponse = await _adminClient.GetAsync("/api/admin/makeboldspark/mail-settings");
        listResponse.EnsureSuccessStatusCode();
        using var listDocument = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var mailSetting = listDocument.RootElement.EnumerateArray()
            .First(item => item.GetProperty("id").GetInt32() == mailSettingId);
        Assert.IsFalse(mailSetting.TryGetProperty("userPassword", out _));
    }

    [TestMethod]
    [DataRow("/api/public/makeboldspark/domains/99999")]
    [DataRow("/api/public/makeboldspark/blogs/99999")]
    [DataRow("/api/public/makeboldspark/authors/99999")]
    [DataRow("/api/public/makeboldspark/posts/99999")]
    [DataRow("/api/public/makeboldspark/categories/99999")]
    [DataRow("/api/public/makeboldspark/menus/99999")]
    [DataRow("/api/public/makeboldspark/keywords/99999")]
    [DataRow("/api/public/makeboldspark/content-parts/99999")]
    public async Task PublicMakeBoldSparkDetailEndpoints_NonExistent_ReturnNotFound(string path)
    {
        var response = await _anonClient.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Admin endpoints: unauthenticated → 401 ───────────────────────────────

    [TestMethod]
    [DataRow("POST",   "/api/admin/makeboldspark/domains")]
    [DataRow("PUT",    "/api/admin/makeboldspark/domains/1")]
    [DataRow("DELETE", "/api/admin/makeboldspark/domains/1")]
    [DataRow("POST",   "/api/admin/makeboldspark/blogs")]
    [DataRow("POST",   "/api/admin/makeboldspark/authors")]
    [DataRow("POST",   "/api/admin/makeboldspark/posts")]
    [DataRow("GET",    "/api/admin/makeboldspark/subscribers")]
    [DataRow("POST",   "/api/admin/makeboldspark/subscribers")]
    [DataRow("GET",    "/api/admin/makeboldspark/newsletters")]
    [DataRow("GET",    "/api/admin/makeboldspark/mail-settings")]
    public async Task AdminMakeBoldSparkEndpoints_WithoutAuth_ReturnUnauthorized(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });

        var response = await _anonClient.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Admin endpoints: authenticated Admin → authorization accepted ─────────

    [TestMethod]
    [DataRow("/api/admin/makeboldspark/subscribers")]
    [DataRow("/api/admin/makeboldspark/newsletters")]
    [DataRow("/api/admin/makeboldspark/mail-settings")]
    public async Task AdminMakeBoldSparkGetEndpoints_WithAdminAuth_ReturnOk(string path)
    {
        var response = await _adminClient.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    [DataRow("/api/admin/makeboldspark/domains")]
    [DataRow("/api/admin/makeboldspark/blogs")]
    [DataRow("/api/admin/makeboldspark/authors")]
    [DataRow("/api/admin/makeboldspark/posts")]
    [DataRow("/api/admin/makeboldspark/categories")]
    [DataRow("/api/admin/makeboldspark/menus")]
    [DataRow("/api/admin/makeboldspark/keywords")]
    [DataRow("/api/admin/makeboldspark/content-parts")]
    public async Task AdminMakeBoldSparkCreateEndpoints_WithAdminAuth_IsAuthorizationAccepted(string path)
    {
        // Verifies Admin role is accepted by the authorization layer.
        // The request may fail with 400/422 due to missing required fields in the body —
        // that is a data concern, not an authorization concern. We assert it is NOT 401 or 403.
        var response = await _adminClient.PostAsJsonAsync(path, new { });
        Assert.AreNotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreNotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
