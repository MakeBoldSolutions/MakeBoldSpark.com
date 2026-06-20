using MakeBoldSpark.Recipe.Data;

namespace MakeBoldSpark.Recipe.Helpers;

public static class RecipeHelper
{
    public static RecipeCategory GetRecipeCategory(string categoryName, int displayOrder)
    {
        return new RecipeCategory()
        {
            DisplayOrder = displayOrder,
            IsActive = true,
            Comment = categoryName,
            Name = categoryName
        };
    }

    /// <summary>
    /// Creates a Recipe entity. Takes domainId (int) instead of WebSite navigation
    /// to keep MakeBoldSpark.Recipe independent of MakeBoldSpark.Core.
    /// </summary>
    public static RecipeEntity GetRecipe(
        int domainId,
        string name,
        string authorName,
        string description,
        string ingredients,
        string instructions,
        RecipeCategory category,
        string keyWords = "")
    {
        return new RecipeEntity()
        {
            Name = name,
            AuthorName = authorName,
            Description = description,
            Keywords = string.IsNullOrWhiteSpace(keyWords) ? name : keyWords,
            Ingredients = ingredients,
            Instructions = instructions,
            DomainId = domainId,
            RecipeCategory = category,
        };
    }
}
