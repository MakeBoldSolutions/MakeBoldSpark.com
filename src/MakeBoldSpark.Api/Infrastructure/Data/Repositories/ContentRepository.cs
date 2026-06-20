using MakeBoldSpark.Api.Features.PublicContent;
using MakeBoldSpark.Api.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MakeBoldSpark.Api.Infrastructure.Data.Repositories;

public class ContentRepository(MakeBoldSparkDbContext db, ILogger<ContentRepository> logger) : IContentRepository
{
    public async Task<IReadOnlyList<ArticleSummary>> GetPublishedArticlesAsync(CancellationToken ct = default)
    {
        try
        {
            // SQLite does not support DateTimeOffset in ORDER BY — sort in memory after fetch
            var articles = await db.Articles
                .Where(a => a.Status == ArticleStatus.Published)
                .Include(a => a.Tags)
                .ToListAsync(ct);

            return articles
                .OrderByDescending(a => a.PublishDate)
                .Select(a => new ArticleSummary(
                    a.Slug,
                    a.Title,
                    a.Summary,
                    a.PublishDate,
                    a.Tags.Select(t => t.Name).ToList()))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve published articles — returning empty list");
            return [];
        }
    }

    public async Task<ArticleDetail?> GetPublishedArticleBySlugAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            var article = await db.Articles
                .Where(a => a.Slug == slug && a.Status == ArticleStatus.Published)
                .Include(a => a.Tags)
                .FirstOrDefaultAsync(ct);

            if (article is null) return null;

            return new ArticleDetail(
                article.Slug,
                article.Title,
                article.Summary,
                article.Body,
                article.PublishDate,
                article.Tags.Select(t => t.Name).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve article '{Slug}' — returning null", slug);
            return null;
        }
    }

    public async Task<IReadOnlyList<TagResponse>> GetAllTagsAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.Tags
                .OrderBy(t => t.Name)
                .Select(t => new TagResponse(t.Name))
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve tags — returning empty list");
            return [];
        }
    }
}
