using MakeBoldSpark.Api.Features.PublicContent;

namespace MakeBoldSpark.Api.Infrastructure.Data.Repositories;

public interface IContentRepository
{
    Task<IReadOnlyList<ArticleSummary>> GetPublishedArticlesAsync(CancellationToken ct = default);
    Task<ArticleDetail?> GetPublishedArticleBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<TagResponse>> GetAllTagsAsync(CancellationToken ct = default);
}
