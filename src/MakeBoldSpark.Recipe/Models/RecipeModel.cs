using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MakeBoldSpark.Recipe.Constants;
using MakeBoldSpark.Recipe.Helpers;

namespace MakeBoldSpark.Recipe.Models;

public class RecipeModel
{
    public RecipeModel()
    {
        RecipeCategory = new RecipeCategoryModel();
    }

    [DisplayName("Author")]
    [Required]
    public string AuthorNM { get; set; } = string.Empty;

    [DisplayName("Average Ratings")]
    public double AverageRating { get; set; }

    [DisplayName("Comments")]
    public int CommentCount { get; set; }

    public string FileDescription { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    [DisplayName("Ingredients")]
    public string Ingredients { get; set; } = string.Empty;

    [DisplayName("Instructions")]
    public string Instructions { get; set; } = string.Empty;

    [DisplayName("Approved")]
    public bool IsApproved { get; set; }

    [DisplayName("Last View")]
    public DateTime LastViewDT { get; set; }

    [DisplayName("Last Modified")]
    public DateTime ModifiedDT { get; set; }

    public int ModifiedID { get; set; }

    [DisplayName("Ratings Count")]
    public int RatingCount { get; set; }

    [DisplayName("Servings")]
    public int Servings { get; set; }

    public RecipeCategoryModel RecipeCategory { get; set; }

    [DisplayName("Category")]
    [Required]
    public int RecipeCategoryID { get; set; }

    [DisplayName("Category")]
    public string RecipeCategoryNM { get; set; } = string.Empty;

    [DisplayName("Description")]
    [Required]
    public string Description { get; set; } = string.Empty;

    public int Id { get; set; }

    [DisplayName("Recipe")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [DisplayName("View Count")]
    public int ViewCount { get; set; }

    public string RecipeURL { get; set; } = string.Empty;

    public List<RecipeImageModel> Images { get; set; } = [];

    public IEnumerable<RecipeOptionModel> RecipeCategories { get; set; } = Array.Empty<RecipeOptionModel>();

    public int DomainID { get; set; } = RecipeConstants.INT_MOM_DomainId;

    public string SEO_Keywords { get; set; } = string.Empty;
    public int Version { get; set; }
}
