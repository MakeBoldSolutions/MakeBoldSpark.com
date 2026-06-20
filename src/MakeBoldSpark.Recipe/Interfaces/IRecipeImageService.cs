using MakeBoldSpark.Recipe.Models;

namespace MakeBoldSpark.Recipe.Interfaces;

public interface IRecipeImageService
{
    void AddRecipeImage(RecipeImageModel recipeImageModel);
    void DeleteRecipeImage(int id);
    RecipeImageModel? GetRecipeImageById(int id);
    IEnumerable<RecipeImageModel> GetRecipeImages();
    void UpdateRecipeImage(RecipeImageModel recipeImageModel);
}
