using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MakeBoldSpark.Api.Infrastructure.OpenApi;
using MakeBoldSpark.Recipe.Models;

namespace MakeBoldSpark.Api.Features.Recipe;

public static class RecipeEndpoints
{
    public static RouteGroupBuilder MapPublicRecipeApi(this RouteGroupBuilder group)
    {
        group.MapGet("/recipes", (RecipeService svc) =>
            Results.Ok(svc.GetApprovedRecipes()))
            .WithName("GetApprovedRecipes")
            .WithTags(MakeBoldSparkOpenApiTags.RecipesCatalog)
            .WithSummary("List all approved recipes")
            .WithDescription("Returns every recipe that has been approved for public display, including ingredients and preparation steps.")
            .Produces<IEnumerable<RecipeModel>>(200)
            .AllowAnonymous();

        group.MapGet("/recipes/{id:int}", (
            [Range(1, int.MaxValue)]
            [Description("Recipe identifier returned by the recipe list endpoint.")]
            [DefaultValue(1)]
            int id,
            RecipeService svc) =>
        {
            var recipe = svc.GetRecipeById(id);
            return recipe is null
                ? Results.NotFound(new { message = $"Recipe {id} not found." })
                : Results.Ok(recipe);
        })
        .WithName("GetRecipeById")
        .WithTags(MakeBoldSparkOpenApiTags.RecipesCatalog)
        .WithSummary("Get a recipe by ID")
        .WithDescription("Fetches the full detail for a single approved recipe identified by its integer ID. Returns 404 if the recipe does not exist or is not approved.")
        .Produces<RecipeModel>(200)
        .Produces(404)
        .AllowAnonymous();

        group.MapGet("/recipes/categories", (RecipeService svc) =>
            Results.Ok(svc.GetCategories()))
            .WithName("GetRecipeCategories")
            .WithTags(MakeBoldSparkOpenApiTags.RecipesCategories)
            .WithSummary("List all recipe categories")
            .WithDescription("Returns the full list of recipe categories used to organise recipes.")
            .Produces<IEnumerable<RecipeCategoryModel>>(200)
            .AllowAnonymous();

        return group;
    }

    public static RouteGroupBuilder MapPublishRecipeApi(this RouteGroupBuilder group)
    {
        group.MapPost("/recipes", (RecipeModel model, RecipeService svc) =>
        {
            var created = svc.CreateRecipe(model);
            return Results.Created($"/api/public/recipes/{created.Id}", created);
        })
        .WithName("CreateRecipe")
        .WithTags(MakeBoldSparkOpenApiTags.RecipesPublishing)
        .WithSummary("Create a new recipe")
        .WithDescription("Creates a new recipe record. The caller must have the Publisher role. Returns the created recipe with its assigned ID.")
        .Produces<RecipeModel>(201)
        .ProducesProblem(400);

        group.MapPut("/recipes/{id:int}", (
            [Range(1, int.MaxValue)]
            [Description("Recipe identifier to update.")]
            [DefaultValue(1)]
            int id,
            RecipeModel model,
            RecipeService svc) =>
        {
            var updated = svc.UpdateRecipe(id, model);
            return updated is null
                ? Results.NotFound(new { message = $"Recipe {id} not found." })
                : Results.Ok(updated);
        })
        .WithName("UpdateRecipe")
        .WithTags(MakeBoldSparkOpenApiTags.RecipesPublishing)
        .WithSummary("Update an existing recipe")
        .WithDescription("Replaces all fields of the specified recipe with the values in the request body. Returns 404 if the recipe does not exist.")
        .Produces<RecipeModel>(200)
        .Produces(404);

        group.MapDelete("/recipes/{id:int}", (
            [Range(1, int.MaxValue)]
            [Description("Recipe identifier to delete.")]
            [DefaultValue(1)]
            int id,
            RecipeService svc) =>
            svc.DeleteRecipe(id)
                ? Results.NoContent()
            : Results.NotFound(new { message = $"Recipe {id} not found." }))
            .WithName("DeleteRecipe")
            .WithTags(MakeBoldSparkOpenApiTags.RecipesPublishing)
            .WithSummary("Delete a recipe")
            .WithDescription("Permanently removes the specified recipe. Returns 204 on success or 404 if not found.")
            .Produces(204)
            .Produces(404);

        group.MapPost("/recipes/categories", (RecipeCategoryModel model, RecipeService svc) =>
        {
            var created = svc.CreateCategory(model);
            return Results.Created($"/api/public/recipes/categories/{created.Id}", created);
        })
        .WithName("CreateRecipeCategory")
        .WithTags(MakeBoldSparkOpenApiTags.RecipesCategories)
        .WithSummary("Create a new recipe category")
        .WithDescription("Creates a new recipe category. Returns the created category with its assigned ID.")
        .Produces<RecipeCategoryModel>(201)
        .ProducesProblem(400);

        group.MapPut("/recipes/categories/{id:int}", (
            [Range(1, int.MaxValue)]
            [Description("Recipe category identifier to update.")]
            [DefaultValue(1)]
            int id,
            RecipeCategoryModel model,
            RecipeService svc) =>
        {
            var updated = svc.UpdateCategory(id, model);
            return updated is null
                ? Results.NotFound(new { message = $"Category {id} not found." })
                : Results.Ok(updated);
        })
        .WithName("UpdateRecipeCategory")
        .WithTags(MakeBoldSparkOpenApiTags.RecipesCategories)
        .WithSummary("Update a recipe category")
        .WithDescription("Replaces all fields of the specified category. Returns 404 if the category does not exist.")
        .Produces<RecipeCategoryModel>(200)
        .Produces(404);

        group.MapDelete("/recipes/categories/{id:int}", (
            [Range(1, int.MaxValue)]
            [Description("Recipe category identifier to delete.")]
            [DefaultValue(1)]
            int id,
            RecipeService svc) =>
            svc.DeleteCategory(id)
                ? Results.NoContent()
            : Results.NotFound(new { message = $"Category {id} not found." }))
            .WithName("DeleteRecipeCategory")
            .WithTags(MakeBoldSparkOpenApiTags.RecipesCategories)
            .WithSummary("Delete a recipe category")
            .WithDescription("Permanently removes the specified category. Returns 204 on success or 404 if not found.")
            .Produces(204)
            .Produces(404);

        return group;
    }
}
