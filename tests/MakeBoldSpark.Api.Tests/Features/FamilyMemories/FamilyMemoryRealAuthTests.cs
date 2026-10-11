using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using MakeBoldSpark.Api.Tests.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MakeBoldSpark.Api.Tests.Features.FamilyMemories;

[TestClass]
public class FamilyMemoryRealAuthTests
{
    [TestMethod]
    public async Task Existing_CMS_login_token_manages_memories_and_invalid_token_is_rejected()
    {
        var factory = new MakeBoldSparkRealAuthWebApplicationFactory();
        await factory.InitializeAsync();
        try
        {
            // Minimal-host ConfigureAppConfiguration runs after the app's eager auth registration.
            // Align the validator with the fixture's issuing key instead of workstation user secrets.
            using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(MakeBoldSparkRealAuthWebApplicationFactory.TestSigningKey)))));
            using var client = configured.CreateClient();
            await factory.SeedAuthorAsync("fictional-memory-editor@example.com", "Fictional-test-password-123!", true);
            const string adminPath = "/api/admin/makeboldspark/family-memories";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-test-token");
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync(adminPath)).StatusCode);
            client.DefaultRequestHeaders.Authorization = null;
            var login = await client.PostAsJsonAsync("/api/public/auth/login", new { email = "fictional-memory-editor@example.com", password = "Fictional-test-password-123!" });
            Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
            var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var created = await client.PostAsJsonAsync(adminPath, new { personName = "Fictional Contributor", personPronouns = "", relationshipToJared = "Fictional friend", body = "Fictional memory." });
            Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);
            Assert.AreEqual(HttpStatusCode.NoContent, (await client.DeleteAsync(created.Headers.Location)).StatusCode);
        }
        finally { await factory.DisposeAsync(); }
    }
}
