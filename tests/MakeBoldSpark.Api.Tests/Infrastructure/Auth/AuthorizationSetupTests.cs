using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MakeBoldSpark.Api.Tests.Infrastructure.Auth;

/// <summary>
/// Regression guard for tasks.md T008a/T009 (gate finding critic-003): the host must fail fast
/// at startup when neither Jwt:Authority nor Jwt:SigningKey is configured, rather than silently
/// falling back to an open admin gate. Explicitly overrides both settings to empty regardless of
/// the ambient environment, since local dev's user-secrets already provides a real
/// Jwt:SigningKey (tasks.md T006) which would otherwise mask this test.
/// </summary>
[TestClass]
public class AuthorizationSetupTests
{
    [TestMethod]
    public void Startup_WithNoSigningConfiguration_ThrowsAtStartup()
    {
        const string signingKeyVariable = "Jwt__SigningKey";
        var originalSigningKey = Environment.GetEnvironmentVariable(signingKeyVariable);
        Environment.SetEnvironmentVariable(signingKeyVariable, string.Empty);

        try
        {
            using var factory = new NoJwtConfigWebApplicationFactory();

            try
            {
                factory.CreateClient();
                Assert.Fail("Expected host startup to throw InvalidOperationException when neither Jwt:Authority nor Jwt:SigningKey is configured.");
            }
            catch (Exception ex)
            {
                var cause = ex;
                while (cause.InnerException is not null) cause = cause.InnerException;
                Assert.IsInstanceOfType<InvalidOperationException>(cause);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(signingKeyVariable, originalSigningKey);
        }
    }

    private sealed class NoJwtConfigWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Authority"] = "",
                    ["Jwt:Audience"] = "",
                    ["Jwt:SigningKey"] = "",
                });
            });
        }
    }
}
