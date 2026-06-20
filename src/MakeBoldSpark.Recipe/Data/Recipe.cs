namespace MakeBoldSpark.Recipe.Data;

public partial class Recipe : RecipeBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int Servings { get; set; }
    public string Ingredients { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public int ViewCount { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime LastViewDt { get; set; }
    public int? DomainId { get; set; }
    public virtual RecipeCategory RecipeCategory { get; set; } = null!;
    public virtual ICollection<RecipeComment> RecipeComment { get; set; } = [];
    public virtual ICollection<RecipeImage> RecipeImage { get; set; } = [];
}
