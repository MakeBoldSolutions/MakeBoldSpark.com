namespace MakeBoldSpark.Recipe.Models;

public class RecipeOptionModel
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsSelected { get; set; }
}
