using MakeBoldSpark.Api.Infrastructure.Data.Repositories;

namespace MakeBoldSpark.Api.Features.PublicContent;

public class ContentService(IContentRepository repository)
{
    public Task<IReadOnlyList<ArticleSummary>> GetPublishedArticlesAsync(CancellationToken ct = default)
        => repository.GetPublishedArticlesAsync(ct);

    public Task<ArticleDetail?> GetPublishedArticleBySlugAsync(string slug, CancellationToken ct = default)
        => repository.GetPublishedArticleBySlugAsync(slug, ct);

    public Task<IReadOnlyList<TagResponse>> GetAllTagsAsync(CancellationToken ct = default)
        => repository.GetAllTagsAsync(ct);
}
