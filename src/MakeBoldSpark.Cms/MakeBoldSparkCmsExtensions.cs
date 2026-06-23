using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace MakeBoldSpark.Cms;

public static class MakeBoldSparkCmsExtensions
{
    public static WebApplication MapMakeBoldSparkCms(this WebApplication app)
    {
        var fileProvider = new ManifestEmbeddedFileProvider(
            typeof(MakeBoldSparkCmsExtensions).Assembly, "build");

        var contentTypes = new FileExtensionContentTypeProvider();
        app.MapGet("/cms/assets/{**assetPath}", (string? assetPath) =>
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return Results.NotFound();

            var file = fileProvider.GetFileInfo($"/assets/{assetPath}");
            if (!file.Exists || file.IsDirectory)
                return Results.NotFound();

            if (!contentTypes.TryGetContentType(file.Name, out var contentType))
                contentType = "application/octet-stream";

            return Results.File(file.CreateReadStream(), contentType, enableRangeProcessing: true);
        });

        // MapFallbackToFile rewrites the matched request path to the bare file name
        // ("index.html", no "/cms" prefix) before serving it, so this second StaticFileOptions
        // must NOT set RequestPath — otherwise the inner static-file handler expects the
        // rewritten path to start with "/cms" and never matches, producing a 404.
        app.MapFallbackToFile("/cms/{*path}", "index.html", new StaticFileOptions
        {
            FileProvider = fileProvider,
        });

        return app;
    }
}
