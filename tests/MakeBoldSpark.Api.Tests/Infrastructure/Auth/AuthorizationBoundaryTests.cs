using System.Net;
using MakeBoldSpark.Api.Tests.Infrastructure;

namespace MakeBoldSpark.Api.Tests.Infrastructure.Auth;

[TestClass]
public class AuthorizationBoundaryTests
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

    [TestMethod]
    public async Task AdminRoute_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/health/deep");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task AdminRoute_WithAdminRole_Returns200()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/admin/health/deep");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task AdminRoute_WithNonAdminRole_Returns403()
    {
        var client = _factory.CreatePublisherClient();
        var response = await client.GetAsync("/api/admin/health/deep");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task PublicRoute_WithoutAuth_Returns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/public/content/articles");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
