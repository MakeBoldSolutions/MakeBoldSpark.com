using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using MakeBoldSpark.Api.Infrastructure.OpenApi;

namespace MakeBoldSpark.Api.Features.PublicContent;

public static partial class PublicContentEndpoints
{
    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]{0,198}[a-z0-9]$|^[a-z0-9]$")]
    private static partial Regex SlugRegex();

    public static RouteGroupBuilder MapPublicContentApi(this RouteGroupBuilder group)
    {
        group.MapGet("/content/articles", async (ContentService svc, CancellationToken ct) =>
        {
            var articles = await svc.GetPublishedArticlesAsync(ct);
            return Results.Ok(articles);
        })
        .WithName("GetPublishedArticles")
        .WithTags(MakeBoldSparkOpenApiTags.PublicContentArticles)
        .WithSummary("List all published articles")
        .WithDescription("Returns summary metadata for every article currently in Published status, ordered by publish date descending. No authentication is required.")
        .Produces<IEnumerable<ArticleSummary>>(200)
        .AllowAnonymous();

        group.MapGet("/content/articles/{slug}", async (
            [RegularExpression("^[a-z0-9][a-z0-9-]{0,198}[a-z0-9]$|^[a-z0-9]$")]
            [StringLength(200, MinimumLength = 1)]
            [Description("URL-safe published article slug.")]
            [DefaultValue("async-openapi-tips")]
            string slug,
            ContentService svc,
            CancellationToken ct) =>
        {
            if (!SlugRegex().IsMatch(slug))
                return Results.Problem("Invalid slug format. Slugs must be lowercase alphanumeric with hyphens, max 200 characters.", statusCode: 400);

            var article = await svc.GetPublishedArticleBySlugAsync(slug, ct);
            return article is null
                ? Results.NotFound(new { message = $"Article '{slug}' not found." })
                : Results.Ok(article);
        })
        .WithName("GetArticleBySlug")
        .WithTags(MakeBoldSparkOpenApiTags.PublicContentArticles)
        .WithSummary("Get a published article by slug")
        .WithDescription("Fetches the full body and metadata of a single published article identified by its URL-safe slug. Returns 400 if the slug format is invalid and 404 if no matching published article exists.")
        .Produces<ArticleDetail>(200)
        .ProducesProblem(400)
        .Produces(404)
        .AllowAnonymous();

        group.MapGet("/content/tags", async (ContentService svc, CancellationToken ct) =>
        {
            var tags = await svc.GetAllTagsAsync(ct);
            return Results.Ok(tags);
        })
        .WithName("GetAllTags")
        .WithTags(MakeBoldSparkOpenApiTags.PublicContentTags)
        .WithSummary("List all content tags")
        .WithDescription("Returns the complete list of taxonomy tags used to classify articles. Useful for building tag-cloud UIs or filter menus.")
        .Produces<IEnumerable<TagResponse>>(200)
        .AllowAnonymous();

        return group;
    }
}
