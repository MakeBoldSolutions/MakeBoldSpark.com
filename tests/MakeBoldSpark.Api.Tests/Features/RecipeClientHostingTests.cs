using System.Net;
using System.Text.RegularExpressions;
using MakeBoldSpark.Api.Tests.Infrastructure;

namespace MakeBoldSpark.Api.Tests.Features;

[TestClass]
public sealed class RecipeClientHostingTests
{
    [TestMethod]
    public async Task RecipeClientEmbeddedAssets_AreServedFromRecipesPath()
    {
        await using var factory = new MakeBoldSparkWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/recipes");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        var stylesheetPath = Regex.Match(html, "href=\"(?<path>/recipes/assets/[^\"]+\\.css)\"")
            .Groups["path"].Value;
        var scriptPath = Regex.Match(html, "src=\"(?<path>/recipes/assets/[^\"]+\\.js)\"")
            .Groups["path"].Value;
        Assert.IsFalse(string.IsNullOrEmpty(stylesheetPath), "The recipe client index should reference its generated stylesheet.");
        Assert.IsFalse(string.IsNullOrEmpty(scriptPath), "The recipe client index should reference its generated JavaScript asset.");

        var stylesheetResponse = await client.GetAsync(stylesheetPath);
        Assert.AreEqual(HttpStatusCode.OK, stylesheetResponse.StatusCode);
        StringAssert.Contains(stylesheetResponse.Content.Headers.ContentType?.MediaType, "css");
        Assert.IsTrue((await stylesheetResponse.Content.ReadAsByteArrayAsync()).Length > 0);

        var scriptResponse = await client.GetAsync(scriptPath);
        Assert.AreEqual(HttpStatusCode.OK, scriptResponse.StatusCode);
        StringAssert.Contains(scriptResponse.Content.Headers.ContentType?.MediaType, "javascript");
        Assert.IsTrue((await scriptResponse.Content.ReadAsByteArrayAsync()).Length > 0);
    }
}
