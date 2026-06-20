using MakeBoldSpark.Recipe.Models;

namespace MakeBoldSpark.Recipe.Interfaces;

/// <summary>
/// Recipe Service Interface.
/// </summary>
/// <remarks>
/// GetRecipeVMHostAsync has been intentionally removed from this interface — it was a
/// presentation-layer concern. Portal callers must construct RecipeVM directly using
/// Get() and GetRecipeCategoryList().
/// </remarks>
public interface IRecipeService
{
    bool Delete(int Id);
    bool Delete(RecipeCategoryModel saveItem);
    RecipeModel Get(int Id);
    RecipeCategoryModel GetRecipeCategoryById(int Id);
    List<RecipeCategoryModel> GetRecipeCategoryList();
    IEnumerable<RecipeModel> Get();
    RecipeModel Save(RecipeModel saveItem);
    IEnumerable<RecipeModel> Save(List<RecipeModel>? saveRecipes);
    RecipeCategoryModel Save(RecipeCategoryModel saveItem);
    List<RecipeCategoryModel> Save(List<RecipeCategoryModel>? saveCategories);
    List<RecipeImageModel> GetRecipeImages();
}
