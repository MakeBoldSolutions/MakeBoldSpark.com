using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MakeBoldSpark.Recipe.Constants;
using MakeBoldSpark.Recipe.Helpers;

namespace MakeBoldSpark.Recipe.Models;

public class RecipeCategoryModel
{
    public RecipeCategoryModel()
    {
        Recipes = [];
    }

    public int Id { get; set; }

    [JsonPropertyName("name")]
    [Display(Name = "Category")]
    [StringLength(50, ErrorMessage = "Max length is 50.")]
    [DataType(DataType.Text)]
    [Required]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [Display(Name = "Description")]
    [StringLength(100, ErrorMessage = "Max length is 100.")]
    [DataType(DataType.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    [Display(Name = "Order")]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public List<RecipeModel> Recipes { get; set; }

    public string Url { get; set; } = string.Empty;

    public int DomainID { get; set; } = RecipeConstants.INT_MOM_DomainId;
}
