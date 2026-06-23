using MakeBoldSpark.Api.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MakeBoldSpark.Api.Tests.Infrastructure.Auth;

/// <summary>
/// Regression guard for tasks.md T008a/T009 (gate finding critic-003): the host must fail fast
/// at startup when neither Jwt:Authority nor Jwt:SigningKey is configured, rather than silently
/// falling back to an open admin gate. The test uses an isolated configuration so local
/// user-secrets and CI environment variables cannot mask the missing-settings case.
/// </summary>
[TestClass]
public class AuthorizationSetupTests
{
    [TestMethod]
    public void Startup_WithNoSigningConfiguration_ThrowsAtStartup()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Authority"] = "",
                ["Jwt:Audience"] = "",
                ["Jwt:SigningKey"] = "",
            })
            .Build();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => services.AddMakeBoldSparkAuth(configuration, null!));
    }
}
