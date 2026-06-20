using Microsoft.EntityFrameworkCore;
using MakeBoldSpark.Recipe.Data;
using MakeBoldSpark.Recipe.Interfaces;
using MakeBoldSpark.Recipe.Models;

namespace MakeBoldSpark.Recipe.Providers;

public class RecipeImageService(RecipeDbContext dbContext) : IRecipeImageService, IDisposable
{
    private bool disposedValue;

    private static RecipeImage ConvertToEntity(RecipeImageModel recipeImageModel, RecipeImage? recipeImage = null)
    {
        recipeImage ??= new RecipeImage();
        recipeImage.FileName = recipeImageModel.FileName;
        recipeImage.FileDescription = recipeImageModel.FileDescription;
        recipeImage.DisplayOrder = recipeImageModel.DisplayOrder;
        recipeImage.Id = recipeImageModel.Recipe.Id;
        recipeImage.ImageData = recipeImageModel.ImageData;
        return recipeImage;
    }

    private static RecipeImageModel ConvertToModel(RecipeImage recipeImage)
    {
        return new RecipeImageModel
        {
            Id = recipeImage.Id,
            FileName = recipeImage.FileName,
            FileDescription = recipeImage.FileDescription,
            DisplayOrder = recipeImage.DisplayOrder,
            ImageData = recipeImage.ImageData,
            Recipe = new RecipeModel
            {
                Id = recipeImage.Recipe?.Id ?? 0,
                Name = recipeImage.Recipe?.Name ?? string.Empty,
                Description = recipeImage.Recipe?.Description ?? string.Empty
            }
        };
    }

    public void AddRecipeImage(RecipeImageModel recipeImageModel)
    {
        var recipeImage = ConvertToEntity(recipeImageModel);
        dbContext.RecipeImage.Add(recipeImage);
        dbContext.SaveChanges();
    }

    public void DeleteRecipeImage(int id)
    {
        var recipeImage = dbContext.RecipeImage.SingleOrDefault(r => r.Id == id);
        if (recipeImage != null)
        {
            dbContext.RecipeImage.Remove(recipeImage);
            dbContext.SaveChanges();
        }
    }

    public RecipeImageModel? GetRecipeImageById(int id)
    {
        var recipeImage = dbContext.RecipeImage
            .Include(r => r.Recipe)
            .SingleOrDefault(r => r.Id == id);
        return recipeImage != null ? ConvertToModel(recipeImage) : null;
    }

    public IEnumerable<RecipeImageModel> GetRecipeImages()
    {
        return dbContext.RecipeImage
            .Include(r => r.Recipe)
            .OrderBy(r => r.DisplayOrder)
            .ToList()
            .Select(r => ConvertToModel(r));
    }

    public void UpdateRecipeImage(RecipeImageModel recipeImageModel)
    {
        var existing = dbContext.RecipeImage.SingleOrDefault(r => r.Id == recipeImageModel.Id)
            ?? throw new InvalidOperationException("Recipe image not found.");
        ConvertToEntity(recipeImageModel, existing);
        dbContext.SaveChanges();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing) dbContext?.Dispose();
            disposedValue = true;
        }
    }

    ~RecipeImageService() => Dispose(disposing: false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
