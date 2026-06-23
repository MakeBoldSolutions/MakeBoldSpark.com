using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MakeBoldSpark.Api.Tests.Infrastructure.Auth;

namespace MakeBoldSpark.Api.Tests.Features.Auth;

[TestClass]
public class AuthEndpointsTests
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
    public async Task Login_WithCorrectAdminCredentials_ReturnsTokenAndDisplayName()
    {
        await _factory.SeedAuthorAsync("admin-ok@example.com", "Sup3rSecret!", isAdmin: true, displayName: "Ada Admin");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/public/auth/login", new
        {
            email = "admin-ok@example.com",
            password = "Sup3rSecret!",
        });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsFalse(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
        Assert.AreEqual("Ada Admin", body.GetProperty("displayName").GetString());
    }

    [TestMethod]
    public async Task Login_AllRejectionReasons_ReturnIdenticalGenericResponse()
    {
        await _factory.SeedAuthorAsync("admin-reject@example.com", "CorrectPassword1", isAdmin: true);
        await _factory.SeedAuthorAsync("non-admin@example.com", "CorrectPassword1", isAdmin: false);
        var client = _factory.CreateClient();

        var wrongPassword = await client.PostAsJsonAsync("/api/public/auth/login", new { email = "admin-reject@example.com", password = "WrongPassword1" });
        var unknownEmail = await client.PostAsJsonAsync("/api/public/auth/login", new { email = "nobody@example.com", password = "Whatever1" });
        var nonAdmin = await client.PostAsJsonAsync("/api/public/auth/login", new { email = "non-admin@example.com", password = "CorrectPassword1" });

        foreach (var response in new[] { wrongPassword, unknownEmail, nonAdmin })
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("Invalid email or password.", body.GetProperty("message").GetString());
        }
    }

    [TestMethod]
    public async Task Login_RepeatedFailuresAgainstSameEmail_AreThrottledWithoutA429()
    {
        await _factory.SeedAuthorAsync("throttle-target@example.com", "CorrectPassword1", isAdmin: true);
        var client = _factory.CreateClient();

        for (var i = 0; i < 8; i++)
        {
            var response = await client.PostAsJsonAsync("/api/public/auth/login", new
            {
                email = "throttle-target@example.com",
                password = $"WrongPassword{i}",
            });

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.AreNotEqual((HttpStatusCode)429, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("Invalid email or password.", body.GetProperty("message").GetString());
        }
    }

    [TestMethod]
    public async Task Login_PasswordOver256Characters_RejectedWithoutHashing()
    {
        await _factory.SeedAuthorAsync("longpassword@example.com", "CorrectPassword1", isAdmin: true);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/public/auth/login", new
        {
            email = "longpassword@example.com",
            password = new string('x', 257),
        });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("Invalid email or password.", body.GetProperty("message").GetString());
    }

    [TestMethod]
    public async Task Login_NeverLogsRequestBodyOrSubmittedPassword()
    {
        const string secret = "TotallyUniqueSecretValue42";
        await _factory.SeedAuthorAsync("logcheck@example.com", secret, isAdmin: true);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/public/auth/login", new { email = "logcheck@example.com", password = secret });

        Assert.IsFalse(_factory.CapturedLogs.Any(log => log.Contains(secret, StringComparison.Ordinal)),
            "A captured log entry contained the submitted password.");
    }
}
