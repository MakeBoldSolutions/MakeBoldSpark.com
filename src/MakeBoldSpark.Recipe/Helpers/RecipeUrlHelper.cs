using System.Globalization;

namespace MakeBoldSpark.Recipe.Helpers;

public static class RecipeUrlHelper
{
    public static string GetSafePath(string name)
    {
        return name == null
            ? string.Empty
            : $"{name.Replace("&", "-").Replace("\n", string.Empty).Replace("/", "-").Replace("'", "-").Replace(" ", "-").ToLower(CultureInfo.CurrentCulture)}";
    }

    public static string GetSafePath(string name, string root)
    {
        return $"{GetSafePath(root)}{GetSafePath(name)}";
    }

    public static string GetRecipeURL(string recipeName)
    {
        return $"/recipe/{GetSafePath(recipeName)}";
    }

    public static string GetRecipeCategoryURL(string name)
    {
        return $"/recipe/category/{GetSafePath(name)}";
    }
}
