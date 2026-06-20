using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MakeBoldSpark.Core.Models;

/// <summary>
/// Simple model for AI recipe generation form
/// Contains only the fields needed from the user
/// </summary>
public class MomCreateRequestModel
{
    /// <summary>
    /// Recipe description/prompt from user
    /// </summary>
    [DisplayName("Recipe Description")]
    [Required(ErrorMessage = "Please describe the recipe you want to create")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
  /// Selected recipe category ID
    /// </summary>
    [DisplayName("Category")]
    [Required(ErrorMessage = "Please select a category")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category")]
    public int RecipeCategoryID { get; set; }

    /// <summary>
    /// Lookup list of available recipe categories for dropdown
    /// </summary>
    public IEnumerable<LookupModel> RecipeCategories { get; set; } = Array.Empty<LookupModel>();
}
