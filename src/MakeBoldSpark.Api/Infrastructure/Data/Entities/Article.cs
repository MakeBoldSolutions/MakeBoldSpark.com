namespace MakeBoldSpark.Api.Infrastructure.Data.Entities;

public class Article
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset? PublishDate { get; set; }
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Tag> Tags { get; set; } = [];
}
