using System.ComponentModel.DataAnnotations;

namespace MakeBoldSpark.Recipe.Data;

public class RecipeBaseEntity
{
    [Key]
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public int? UpdatedID { get; set; }
    public int? CreatedID { get; set; }
}
