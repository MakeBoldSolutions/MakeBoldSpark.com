using System.Text.Json.Serialization;

namespace MakeBoldSpark.Core.Data;

public class Category : BaseEntity
{
    [Required]
    [StringLength(120)]
    public string Content { get; set; } = string.Empty;
    [StringLength(255)]
    public string Description { get; set; } = string.Empty;
    [JsonIgnore] public List<PostCategory> PostCategories { get; set; } = new();
}
