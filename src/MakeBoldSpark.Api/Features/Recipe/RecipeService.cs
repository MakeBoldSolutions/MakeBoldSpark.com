using MakeBoldSpark.Recipe.Interfaces;
using MakeBoldSpark.Recipe.Models;

namespace MakeBoldSpark.Api.Features.Recipe;

public class RecipeService(IRecipeService recipeService, ILogger<RecipeService> logger)
{
    public IReadOnlyList<RecipeModel> GetApprovedRecipes()
    {
        try { return recipeService.Get().Where(r => r.IsApproved).ToList(); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve recipes — returning empty list");
            return [];
        }
    }

    public RecipeModel? GetRecipeById(int id)
    {
        try
        {
            var model = recipeService.Get(id);
            return model.Id == 0 ? null : model;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve recipe {Id}", id);
            return null;
        }
    }

    public IReadOnlyList<RecipeCategoryModel> GetCategories()
    {
        try { return recipeService.GetRecipeCategoryList(); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve recipe categories — returning empty list");
            return [];
        }
    }

    public RecipeModel CreateRecipe(RecipeModel model)
        => recipeService.Save(model);

    public RecipeModel? UpdateRecipe(int id, RecipeModel model)
    {
        var existing = recipeService.Get(id);
        if (existing.Id == 0) return null;
        model.Id = id;
        return recipeService.Save(model);
    }

    public bool DeleteRecipe(int id)
        => recipeService.Delete(id);

    public RecipeCategoryModel CreateCategory(RecipeCategoryModel model)
        => recipeService.Save(model);

    public RecipeCategoryModel? UpdateCategory(int id, RecipeCategoryModel model)
    {
        var existing = recipeService.GetRecipeCategoryById(id);
        if (existing.Id == 0) return null;
        model.Id = id;
        return recipeService.Save(model);
    }

    public bool DeleteCategory(int id)
    {
        var category = recipeService.GetRecipeCategoryById(id);
        if (category.Id == 0) return false;
        return recipeService.Delete(category);
    }
}
