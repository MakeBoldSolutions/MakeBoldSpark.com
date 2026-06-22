using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MakeBoldSpark.Api.Tests.Infrastructure.Auth;

/// <summary>
/// Regression test for the AuthorizationSetup.cs tightening (tasks.md T009): guards against
/// reintroducing the removed "accept any well-formed token" fallback. Uses
/// MakeBoldSparkRealAuthWebApplicationFactory, not the TestScheme-bypass factory, because this
/// must exercise the real JWT signature-validation path — see plan.md Implementation Notes.
/// </summary>
[TestClass]
public class AuthorizationSignatureTests
{
    private static MakeBoldSparkRealAuthWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _factory = new MakeBoldSparkRealAuthWebApplicationFactory();
        await _factory.InitializeAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _factory.DisposeAsync();
    }

    [TestMethod]
    public async Task AdminRoute_WithWronglySignedToken_Returns401()
    {
        var token = BuildAdminToken(signingKey: "a-completely-different-key-not-the-real-one-0000");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/health/deep");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task AdminRoute_WithUnsignedToken_Returns401()
    {
        var handler = new JwtSecurityTokenHandler();
        var unsignedToken = handler.WriteToken(new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.Role, "Admin")]));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", unsignedToken);

        var response = await client.GetAsync("/api/admin/health/deep");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string BuildAdminToken(string signingKey)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.Role, "Admin")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
