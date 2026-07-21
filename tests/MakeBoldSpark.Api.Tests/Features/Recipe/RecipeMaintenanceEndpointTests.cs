using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using MakeBoldSpark.Api.Features.Recipe;
using MakeBoldSpark.Api.Tests.Infrastructure;
using MakeBoldSpark.Core.Data;
using MakeBoldSpark.Recipe.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MakeBoldSpark.Api.Tests.Features.Recipe;

[TestClass]
public class RecipeMaintenanceEndpointTests
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

    [TestMethod]
    public async Task Domains_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _anonClient.GetAsync("/api/publish/recipes/domains");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Domains_WithPublisher_ReturnsConfiguredDomain()
    {
        var seeded = await SeedInventoryAsync();

        var domains = await _publisherClient.GetFromJsonAsync<List<MaintenanceDomain>>("/api/publish/recipes/domains");

        Assert.IsNotNull(domains);
        Assert.IsTrue(domains.Any(domain => domain.Id == seeded.DomainId));
    }

    [TestMethod]
    public async Task ListRecipes_WithoutDomainId_ReturnsBadRequest()
    {
        var response = await _publisherClient.GetAsync("/api/publish/recipes");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ListRecipes_WithPublisher_ReturnsBoundedSelectedDomainInventoryIncludingUnapproved()
    {
        var seeded = await SeedInventoryAsync();

        var response = await _publisherClient.GetAsync($"/api/publish/recipes?domainId={seeded.DomainId}&page=0&pageSize=999");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<RecipeMaintenanceRecipe>>();
        Assert.IsNotNull(page);
        Assert.AreEqual(1, page.Page);
        Assert.AreEqual(100, page.PageSize);
        Assert.IsTrue(page.Items.Any(recipe => recipe.Id == seeded.UnapprovedRecipeId && !recipe.IsApproved));
        Assert.IsFalse(page.Items.Any(recipe => recipe.DomainId == seeded.OtherDomainId));
    }

    [TestMethod]
    public async Task GetRecipe_WithDifferentDomain_ReturnsNotFound()
    {
        var seeded = await SeedInventoryAsync();

        var response = await _publisherClient.GetAsync($"/api/publish/recipes/{seeded.RecipeId}?domainId={seeded.OtherDomainId}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateRecipe_WithCategoryFromDifferentDomain_ReturnsBadRequest()
    {
        var seeded = await SeedInventoryAsync();
        var payload = RecipePayload("Cross-domain category", seeded.OtherCategoryId);

        var response = await _publisherClient.PostAsJsonAsync($"/api/publish/recipes?domainId={seeded.DomainId}", payload);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateRecipe_WithOversizedPayload_ReturnsPayloadTooLarge()
    {
        var seeded = await SeedInventoryAsync();
        var json = $$"""
            {
              "name": "{{new string('a', 270000)}}",
              "description": "Too large",
              "authorName": "Tester",
              "ingredients": "Water",
              "instructions": "Stir",
              "servings": 1,
              "recipeCategoryId": {{seeded.CategoryId}},
              "isApproved": false
            }
            """;
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _publisherClient.PostAsync($"/api/publish/recipes?domainId={seeded.DomainId}", content);

        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateRecipe_WithMatchingVersion_UpdatesAndIncrementsVersion()
    {
        var seeded = await SeedInventoryAsync();
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/publish/recipes/{seeded.RecipeId}?domainId={seeded.DomainId}")
        {
            Content = JsonContent.Create(RecipePayload("Updated recipe", seeded.CategoryId))
        };
        request.Headers.IfMatch.Add(EntityTagHeaderValue.Parse($"\"{seeded.RecipeVersion}\""));

        var response = await _publisherClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<RecipeMaintenanceRecipe>();
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated recipe", updated.Name);
        Assert.IsTrue(updated.Version > seeded.RecipeVersion);
    }

    [TestMethod]
    public async Task UpdateRecipe_WithStaleVersion_ReturnsPreconditionFailed()
    {
        var seeded = await SeedInventoryAsync();
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/publish/recipes/{seeded.RecipeId}?domainId={seeded.DomainId}")
        {
            Content = JsonContent.Create(RecipePayload("Stale recipe", seeded.CategoryId))
        };
        request.Headers.IfMatch.Add(EntityTagHeaderValue.Parse("\"999\""));

        var response = await _publisherClient.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateRecipe_WithoutIfMatch_ReturnsBadRequest()
    {
        var seeded = await SeedInventoryAsync();

        var response = await _publisherClient.PutAsJsonAsync(
            $"/api/publish/recipes/{seeded.RecipeId}?domainId={seeded.DomainId}",
            RecipePayload("Missing If-Match", seeded.CategoryId));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteCategory_WhenCategoryIsInUse_ReturnsConflict()
    {
        var seeded = await SeedInventoryAsync();
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/publish/recipes/categories/{seeded.CategoryId}?domainId={seeded.DomainId}");
        request.Headers.IfMatch.Add(EntityTagHeaderValue.Parse($"\"{seeded.CategoryVersion}\""));

        var response = await _publisherClient.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task OpenApi_IncludesRuntimeMaintenanceRoutesAndStatusContracts()
    {
        var response = await _anonClient.GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(body, "/api/publish/recipes");
        StringAssert.Contains(body, "ListPublisherRecipes");
        StringAssert.Contains(body, "412");
        StringAssert.Contains(body, "409");
    }

    private static object RecipePayload(string name, int categoryId) => new
    {
        name,
        description = "Endpoint test recipe",
        authorName = "Tester",
        ingredients = "Water",
        instructions = "Stir",
        servings = 1,
        recipeCategoryId = categoryId,
        isApproved = false
    };

    private static async Task<SeededInventory> SeedInventoryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var recipeDb = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
        var coreDb = scope.ServiceProvider.GetRequiredService<MakeBoldSparkCoreDbContext>();
        var suffix = Guid.NewGuid().ToString("N");

        var domain = new WebSite
        {
            Name = $"Recipe Test {suffix[..8]}",
            Title = $"Recipe Test {suffix}",
            DomainUrl = $"{suffix}.example.test",
            Description = "Recipe maintenance endpoint test domain",
            Template = "test",
            GalleryFolder = "test",
            Style = "test",
            IsRecipeSite = true,
        };
        var otherDomain = new WebSite
        {
            Name = $"Other Recipe {suffix[..8]}",
            Title = $"Other Recipe {suffix}",
            DomainUrl = $"other-{suffix}.example.test",
            Description = "Other recipe maintenance endpoint test domain",
            Template = "test",
            GalleryFolder = "test",
            Style = "test",
            IsRecipeSite = true,
        };
        coreDb.Domain.AddRange(domain, otherDomain);
        await coreDb.SaveChangesAsync();

        var category = new RecipeCategory
        {
            Name = $"Category {suffix}",
            Comment = "In-use category",
            DisplayOrder = 1,
            IsActive = true,
            DomainId = domain.Id,
        };
        var otherCategory = new RecipeCategory
        {
            Name = $"Other Category {suffix}",
            Comment = "Other category",
            DisplayOrder = 1,
            IsActive = true,
            DomainId = otherDomain.Id,
        };
        recipeDb.RecipeCategory.AddRange(category, otherCategory);
        await recipeDb.SaveChangesAsync();

        var approved = new global::MakeBoldSpark.Recipe.Data.Recipe
        {
            Name = $"Approved recipe {suffix}",
            Description = "Approved",
            AuthorName = "Tester",
            Ingredients = "Water",
            Instructions = "Stir",
            Servings = 1,
            IsApproved = true,
            DomainId = domain.Id,
            RecipeCategory = category,
        };
        var unapproved = new global::MakeBoldSpark.Recipe.Data.Recipe
        {
            Name = $"Unapproved recipe {suffix}",
            Description = "Unapproved",
            AuthorName = "Tester",
            Ingredients = "Water",
            Instructions = "Stir",
            Servings = 1,
            IsApproved = false,
            DomainId = domain.Id,
            RecipeCategory = category,
        };
        var otherRecipe = new global::MakeBoldSpark.Recipe.Data.Recipe
        {
            Name = $"Other recipe {suffix}",
            Description = "Other",
            AuthorName = "Tester",
            Ingredients = "Water",
            Instructions = "Stir",
            Servings = 1,
            IsApproved = true,
            DomainId = otherDomain.Id,
            RecipeCategory = otherCategory,
        };
        recipeDb.Recipe.AddRange(approved, unapproved, otherRecipe);
        await recipeDb.SaveChangesAsync();

        return new SeededInventory(
            domain.Id,
            otherDomain.Id,
            category.Id,
            category.Version,
            otherCategory.Id,
            approved.Id,
            approved.Version,
            unapproved.Id);
    }

    private sealed record SeededInventory(
        int DomainId,
        int OtherDomainId,
        int CategoryId,
        int CategoryVersion,
        int OtherCategoryId,
        int RecipeId,
        int RecipeVersion,
        int UnapprovedRecipeId);
}
