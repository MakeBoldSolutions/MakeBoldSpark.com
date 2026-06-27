using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace MakeBoldSpark.Recipe.Client;

public static class MakeBoldSparkRecipeClientExtensions
{
    public static WebApplication MapMakeBoldSparkRecipeClient(this WebApplication app)
    {
        var provider = new ManifestEmbeddedFileProvider(typeof(MakeBoldSparkRecipeClientExtensions).Assembly, "build");
        var contentTypes = new FileExtensionContentTypeProvider();
        app.MapGet("/recipes/assets/{**assetPath}", (string? assetPath) =>
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return Results.NotFound();
            var file = provider.GetFileInfo($"/assets/{assetPath}");
            if (!file.Exists || file.IsDirectory) return Results.NotFound();
            if (!contentTypes.TryGetContentType(file.Name, out var type)) type = "application/octet-stream";
            return Results.File(file.CreateReadStream(), type, enableRangeProcessing: true);
        });
        app.MapFallbackToFile("/recipes/{*path}", "index.html", new StaticFileOptions { FileProvider = provider });
        return app;
    }
}
