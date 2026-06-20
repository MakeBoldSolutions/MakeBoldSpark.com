namespace MakeBoldSpark.Recipe.Data;

public partial class RecipeCategory : RecipeBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int? DomainId { get; set; }
    public virtual ICollection<Recipe> Recipe { get; set; } = [];
}
