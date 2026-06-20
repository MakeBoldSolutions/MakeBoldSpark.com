namespace MakeBoldSpark.Recipe.Models.EditModels;

public class RecipeEditModel : RecipeModel
{
    public RecipeEditModel() { }

    public RecipeEditModel(RecipeModel recipe)
    {
        if (recipe == null) return;
        AuthorNM = recipe.AuthorNM;
        AverageRating = recipe.AverageRating;
        IsApproved = recipe.IsApproved;
        CommentCount = recipe.CommentCount;
        Description = recipe.Description;
        FileDescription = recipe.FileDescription;
        LastViewDT = recipe.LastViewDT;
        FileName = recipe.FileName;
        Id = recipe.Id;
        ModifiedDT = recipe.ModifiedDT;
        RecipeCategoryID = recipe.RecipeCategoryID;
        Ingredients = recipe.Ingredients;
        Instructions = recipe.Instructions;
        ModifiedID = recipe.ModifiedID;
        Servings = recipe.Servings;
        Name = recipe.Name;
        RatingCount = recipe.RatingCount;
        RecipeCategories = recipe.RecipeCategories;
        RecipeCategory = recipe.RecipeCategory;
        RecipeCategoryNM = recipe.RecipeCategoryNM;
        RecipeURL = recipe.RecipeURL;
        ViewCount = recipe.ViewCount;
    }

    public List<RecipeCategoryModel> Categories { get; set; } = [];
}
