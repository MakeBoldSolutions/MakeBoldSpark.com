using System.Net;
using System.Net.Http.Json;
using ApiSpark.Api.Tests.Infrastructure;

namespace ApiSpark.Api.Tests.Features.WebSpark;

[TestClass]
public class WebSparkEndpointTests
{
    private static ApiSparkWebApplicationFactory _factory = null!;
    private HttpClient _anonClient = null!;
    private HttpClient _adminClient = null!;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _factory = new ApiSparkWebApplicationFactory();
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
    [DataRow("/api/public/webspark/domains")]
    [DataRow("/api/public/webspark/blogs")]
    [DataRow("/api/public/webspark/authors")]
    [DataRow("/api/public/webspark/posts")]
    [DataRow("/api/public/webspark/categories")]
    [DataRow("/api/public/webspark/menus")]
    [DataRow("/api/public/webspark/keywords")]
    [DataRow("/api/public/webspark/content-parts")]
    public async Task PublicWebSparkEndpoints_Anonymous_ReturnOk(string path)
    {
        var response = await _anonClient.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    [DataRow("/api/public/webspark/domains")]
    [DataRow("/api/public/webspark/blogs")]
    [DataRow("/api/public/webspark/authors")]
    [DataRow("/api/public/webspark/posts")]
    [DataRow("/api/public/webspark/categories")]
    [DataRow("/api/public/webspark/menus")]
    [DataRow("/api/public/webspark/keywords")]
    [DataRow("/api/public/webspark/content-parts")]
    public async Task PublicWebSparkListEndpoints_ReturnJsonArray(string path)
    {
        var response = await _anonClient.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.IsNotNull(body);
    }

    [TestMethod]
    [DataRow("/api/public/webspark/domains/99999")]
    [DataRow("/api/public/webspark/blogs/99999")]
    [DataRow("/api/public/webspark/authors/99999")]
    [DataRow("/api/public/webspark/posts/99999")]
    [DataRow("/api/public/webspark/categories/99999")]
    [DataRow("/api/public/webspark/menus/99999")]
    [DataRow("/api/public/webspark/keywords/99999")]
    [DataRow("/api/public/webspark/content-parts/99999")]
    public async Task PublicWebSparkDetailEndpoints_NonExistent_ReturnNotFound(string path)
    {
        var response = await _anonClient.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Admin endpoints: unauthenticated → 401 ───────────────────────────────

    [TestMethod]
    [DataRow("POST",   "/api/admin/webspark/domains")]
    [DataRow("PUT",    "/api/admin/webspark/domains/1")]
    [DataRow("DELETE", "/api/admin/webspark/domains/1")]
    [DataRow("POST",   "/api/admin/webspark/blogs")]
    [DataRow("POST",   "/api/admin/webspark/authors")]
    [DataRow("POST",   "/api/admin/webspark/posts")]
    [DataRow("GET",    "/api/admin/webspark/subscribers")]
    [DataRow("POST",   "/api/admin/webspark/subscribers")]
    [DataRow("GET",    "/api/admin/webspark/newsletters")]
    [DataRow("GET",    "/api/admin/webspark/mail-settings")]
    public async Task AdminWebSparkEndpoints_WithoutAuth_ReturnUnauthorized(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });

        var response = await _anonClient.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Admin endpoints: authenticated Admin → authorization accepted ─────────

    [TestMethod]
    [DataRow("/api/admin/webspark/subscribers")]
    [DataRow("/api/admin/webspark/newsletters")]
    [DataRow("/api/admin/webspark/mail-settings")]
    public async Task AdminWebSparkGetEndpoints_WithAdminAuth_ReturnOk(string path)
    {
        var response = await _adminClient.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    [DataRow("/api/admin/webspark/domains")]
    [DataRow("/api/admin/webspark/blogs")]
    [DataRow("/api/admin/webspark/authors")]
    [DataRow("/api/admin/webspark/posts")]
    [DataRow("/api/admin/webspark/categories")]
    [DataRow("/api/admin/webspark/menus")]
    [DataRow("/api/admin/webspark/keywords")]
    [DataRow("/api/admin/webspark/content-parts")]
    public async Task AdminWebSparkCreateEndpoints_WithAdminAuth_IsAuthorizationAccepted(string path)
    {
        // Verifies Admin role is accepted by the authorization layer.
        // The request may fail with 400/422 due to missing required fields in the body —
        // that is a data concern, not an authorization concern. We assert it is NOT 401 or 403.
        var response = await _adminClient.PostAsJsonAsync(path, new { });
        Assert.AreNotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreNotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
