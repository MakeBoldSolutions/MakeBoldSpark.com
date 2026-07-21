using System.Net;
using System.Net.Http.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;
using MakeBoldSpark.Recipe.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MakeBoldSpark.Api.Tests.Features.Recipe;

[TestClass]
public class RecipeEndpointTests
{
    private static MakeBoldSparkWebApplicationFactory _factory = null!;
    private HttpClient _anonClient = null!;
    private HttpClient _publisherClient = null!;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _factory = new MakeBoldSparkWebApplicationFactory();
        await _factory.InitializeAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _factory.DisposeAsync();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _anonClient = _factory.CreateClient();
        _publisherClient = _factory.CreatePublisherClient();
    }

    // ── Public read endpoints (anonymous) ────────────────────────────────────

    [TestMethod]
    public async Task GetRecipes_Anonymous_ReturnsOk()
    {
        var response = await _anonClient.GetAsync("/api/public/recipes");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetRecipes_Anonymous_ReturnsJsonArray()
    {
        var response = await _anonClient.GetAsync("/api/public/recipes");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<object[]>();
        Assert.IsNotNull(body);
    }

    [TestMethod]
    public async Task GetRecipeCategories_Anonymous_ReturnsOk()
    {
        var response = await _anonClient.GetAsync("/api/public/recipes/categories");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task GetRecipeById_NonExistent_ReturnsNotFound()
    {
        var response = await _anonClient.GetAsync("/api/public/recipes/99999");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetRecipeById_Anonymous_UnapprovedRecipeReturnsNotFound()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
        var category = new RecipeCategory
        {
            Name = $"Unapproved category {Guid.NewGuid():N}",
            DomainId = 1,
        };
        db.RecipeCategory.Add(category);
        await db.SaveChangesAsync();

        var recipe = new global::MakeBoldSpark.Recipe.Data.Recipe
        {
            Name = $"Unapproved recipe {Guid.NewGuid():N}",
            AuthorName = "Test Author",
            Description = "Not yet public",
            Ingredients = "Ingredient",
            Instructions = "Instruction",
            IsApproved = false,
            DomainId = 1,
            RecipeCategory = category,
        };
        db.Recipe.Add(recipe);
        await db.SaveChangesAsync();

        var response = await _anonClient.GetAsync($"/api/public/recipes/{recipe.Id}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Publish endpoints: authorization boundary ─────────────────────────────

    [TestMethod]
    public async Task CreateRecipe_WithoutAuth_ReturnsUnauthorized()
    {
        var payload = new { name = "Test Recipe", ingredients = "x", instructions = "y", authorName = "Test", recipeCategoryId = 1 };
        var response = await _anonClient.PostAsJsonAsync("/api/publish/recipes", payload);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateRecipe_WithoutAuth_ReturnsUnauthorized()
    {
        var payload = new { name = "Test Recipe", ingredients = "x", instructions = "y", authorName = "Test", recipeCategoryId = 1 };
        var response = await _anonClient.PutAsJsonAsync("/api/publish/recipes/1", payload);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteRecipe_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _anonClient.DeleteAsync("/api/publish/recipes/1");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateRecipeCategory_WithoutAuth_ReturnsUnauthorized()
    {
        var payload = new { name = "Test Category" };
        var response = await _anonClient.PostAsJsonAsync("/api/publish/recipes/categories", payload);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateRecipe_WithPublisherAuth_IsAuthorizationAccepted()
    {
        // Verifies Publisher role is accepted by the authorization layer (auth check passes).
        // The request may fail with 400/500 if DB has no matching category — that is a data
        // concern, not an authorization concern. We assert it is NOT 401 or 403.
        var payload = new { name = "Test Recipe", ingredients = "x", instructions = "y", authorName = "Test", recipeCategoryId = 1 };
        var response = await _publisherClient.PostAsJsonAsync("/api/publish/recipes", payload);
        Assert.AreNotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreNotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteRecipe_WithPublisherAuth_NonExistent_IsAuthorizationAccepted()
    {
        // Verifies Publisher role is accepted — returns NotFound (not 401/403).
        var response = await _publisherClient.DeleteAsync("/api/publish/recipes/99999");
        Assert.AreNotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreNotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
