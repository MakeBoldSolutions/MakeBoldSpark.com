using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ApiSpark.Api.Features.PublicContent;

/// <summary>Lightweight summary of a published article, suitable for list views.</summary>
/// <param name="Slug">URL-safe identifier unique across all published articles.</param>
/// <param name="Title">Display title of the article.</param>
/// <param name="Summary">Short plain-text description used for cards and search results.</param>
/// <param name="PublishDate">UTC date/time the article was published; null if unpublished.</param>
/// <param name="Tags">Taxonomy tags associated with the article.</param>
public record ArticleSummary(
    [property: Required]
    [property: StringLength(200, MinimumLength = 1)]
    [property: RegularExpression("^[a-z0-9][a-z0-9-]{0,198}[a-z0-9]$|^[a-z0-9]$")]
    [property: DefaultValue("async-openapi-tips")]
    [property: Description("URL-safe identifier unique across all published articles.")]
    string Slug,
    [property: Required]
    [property: StringLength(200, MinimumLength = 1)]
    [property: DefaultValue("Async OpenAPI Testing Tips")]
    [property: Description("Display title of the article.")]
    string Title,
    [property: StringLength(500)]
    [property: DefaultValue("Practical guidance for testing Minimal APIs with rich OpenAPI metadata.")]
    [property: Description("Short plain-text description used for cards and search results.")]
    string Summary,
    [property: Description("UTC date and time the article was published; null when the article has not been published.")]
    DateTimeOffset? PublishDate,
    [property: Description("Taxonomy tags associated with the article.")]
    IReadOnlyList<string> Tags);

/// <summary>Full article content including the rendered body.</summary>
/// <param name="Slug">URL-safe identifier unique across all published articles.</param>
/// <param name="Title">Display title of the article.</param>
/// <param name="Summary">Short plain-text description used for cards and search results.</param>
/// <param name="Body">Full HTML or Markdown body of the article.</param>
/// <param name="PublishDate">UTC date/time the article was published; null if unpublished.</param>
/// <param name="Tags">Taxonomy tags associated with the article.</param>
public record ArticleDetail(
    [property: Required]
    [property: StringLength(200, MinimumLength = 1)]
    [property: RegularExpression("^[a-z0-9][a-z0-9-]{0,198}[a-z0-9]$|^[a-z0-9]$")]
    [property: DefaultValue("async-openapi-tips")]
    [property: Description("URL-safe identifier unique across all published articles.")]
    string Slug,
    [property: Required]
    [property: StringLength(200, MinimumLength = 1)]
    [property: DefaultValue("Async OpenAPI Testing Tips")]
    [property: Description("Display title of the article.")]
    string Title,
    [property: StringLength(500)]
    [property: DefaultValue("Practical guidance for testing Minimal APIs with rich OpenAPI metadata.")]
    [property: Description("Short plain-text description used for cards and search results.")]
    string Summary,
    [property: Required]
    [property: Description("Full HTML or Markdown body of the article.")]
    string Body,
    [property: Description("UTC date and time the article was published; null when the article has not been published.")]
    DateTimeOffset? PublishDate,
    [property: Description("Taxonomy tags associated with the article.")]
    IReadOnlyList<string> Tags);

/// <summary>A single taxonomy tag used to classify content.</summary>
/// <param name="Name">The tag label, e.g. "csharp" or "async".</param>
public record TagResponse(
    [property: Required]
    [property: StringLength(64, MinimumLength = 1)]
    [property: DefaultValue("async")]
    [property: Description("Single taxonomy tag label used to classify content.")]
    string Name);
