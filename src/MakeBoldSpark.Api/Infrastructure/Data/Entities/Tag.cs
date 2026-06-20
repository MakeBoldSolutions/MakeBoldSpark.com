namespace MakeBoldSpark.Api.Infrastructure.Data.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Article> Articles { get; set; } = [];
}
