using System.Net;
using System.Text.RegularExpressions;
using MakeBoldSpark.Api.Tests.Infrastructure;

namespace MakeBoldSpark.Api.Tests.Features;

[TestClass]
public sealed class CmsHostingTests
{
    [TestMethod]
    public async Task CmsEmbeddedJavaScript_IsServedAsJavaScript()
    {
        await using var factory = new MakeBoldSparkWebApplicationFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        var cmsResponse = await client.GetAsync("/cms");
        Assert.AreEqual(HttpStatusCode.OK, cmsResponse.StatusCode);

        var html = await cmsResponse.Content.ReadAsStringAsync();
        var stylesheetPath = Regex.Match(html, "href=\"(?<path>/cms/assets/[^\"]+\\.css)\"")
            .Groups["path"].Value;
        var scriptPath = Regex.Match(html, "src=\"(?<path>/cms/assets/[^\"]+\\.js)\"")
            .Groups["path"].Value;
        Assert.IsFalse(string.IsNullOrEmpty(stylesheetPath), "The CMS index should reference its generated stylesheet.");
        Assert.IsFalse(string.IsNullOrEmpty(scriptPath), "The CMS index should reference its generated JavaScript asset.");

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
