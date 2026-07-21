using MakeBoldSpark.Api.Features.MakeBoldSpark;
using MakeBoldSpark.Recipe.Data;
using MakeBoldSpark.Recipe.Interfaces;
using MakeBoldSpark.Recipe.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MakeBoldSpark.Api.Features.Recipe;

public class RecipeService(IRecipeService legacy, RecipeDbContext db, MakeBoldSparkService domains)
{
    public IReadOnlyList<RecipeModel> GetApprovedRecipes() => legacy.Get().Where(r => r.IsApproved).ToList();
    public RecipeModel? GetRecipeById(int id) { var recipe = legacy.Get(id); return recipe.Id == 0 || !recipe.IsApproved ? null : recipe; }
    public IReadOnlyList<RecipeCategoryModel> GetCategories() => legacy.GetRecipeCategoryList();
    public RecipeModel CreateRecipe(RecipeModel model) => legacy.Save(model);
    public RecipeModel? UpdateRecipe(int id, RecipeModel model) { var current = legacy.Get(id); if (current.Id == 0) return null; model.Id = id; return legacy.Save(model); }
    public bool DeleteRecipe(int id) => legacy.Delete(id);
    public RecipeCategoryModel CreateCategory(RecipeCategoryModel model) => legacy.Save(model);
    public RecipeCategoryModel? UpdateCategory(int id, RecipeCategoryModel model) { var current = legacy.GetRecipeCategoryById(id); if (current.Id == 0) return null; model.Id = id; return legacy.Save(model); }
    public bool DeleteCategory(int id) { var current = legacy.GetRecipeCategoryById(id); return current.Id != 0 && legacy.Delete(current); }
    public async Task<IReadOnlyList<MaintenanceDomain>> GetDomainsAsync(CancellationToken ct) => (await domains.GetDomainsAsync(ct)).Select(x => new MaintenanceDomain(x.Id, x.Name)).ToList();
    public async Task<PagedResult<RecipeMaintenanceRecipe>> ListAsync(int domainId, int page, int size, CancellationToken ct)
    {
        var query = db.Recipe.AsNoTracking().Include(x => x.RecipeCategory).Where(x => x.DomainId == domainId).OrderBy(x => x.Name);
        var total = await query.CountAsync(ct);
        var rows = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<RecipeMaintenanceRecipe>(rows.Select(MapMaintenanceRecipe).ToList(), page, size, total);
    }
    public async Task<RecipeMaintenanceRecipe?> GetAsync(int id, int domainId, CancellationToken ct)
    {
        var row = await db.Recipe.AsNoTracking().Include(x => x.RecipeCategory).FirstOrDefaultAsync(x => x.Id == id && x.DomainId == domainId, ct);
        return row is null ? null : MapMaintenanceRecipe(row);
    }
    public async Task<IReadOnlyList<RecipeMaintenanceCategory>> CategoriesAsync(int domainId, CancellationToken ct)
        => (await db.RecipeCategory.AsNoTracking().Where(x => x.DomainId == domainId).OrderBy(x => x.Name).ToListAsync(ct)).Select(MapMaintenanceCategory).ToList();
    public async Task<RecipeMaintenanceCategory?> GetCategoryAsync(int id, int domainId, CancellationToken ct)
    {
        var row = await db.RecipeCategory.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.DomainId == domainId, ct);
        return row is null ? null : MapMaintenanceCategory(row);
    }
    public async Task<RecipeMaintenanceRecipe> CreateAsync(RecipeWriteRequest request, int domainId, CancellationToken ct)
    {
        var category = await db.RecipeCategory.FirstOrDefaultAsync(x => x.Id == request.RecipeCategoryId && x.DomainId == domainId, ct) ?? throw new ValidationException("Category is not in the selected domain.");
        var row = new global::MakeBoldSpark.Recipe.Data.Recipe { Name = request.Name, Description = request.Description, AuthorName = request.AuthorName, Ingredients = request.Ingredients, Instructions = request.Instructions, Servings = request.Servings, IsApproved = request.IsApproved, DomainId = domainId, RecipeCategory = category };
        db.Recipe.Add(row); await db.SaveChangesAsync(ct); return MapMaintenanceRecipe(row);
    }
    public async Task<RecipeMaintenanceRecipe?> UpdateAsync(int id, RecipeWriteRequest request, int domainId, int version, CancellationToken ct)
    {
        var row = await db.Recipe.Include(x => x.RecipeCategory).FirstOrDefaultAsync(x => x.Id == id && x.DomainId == domainId, ct);
        if (row is null) return null; if (row.Version != version) throw new DbUpdateConcurrencyException();
        var category = await db.RecipeCategory.FirstOrDefaultAsync(x => x.Id == request.RecipeCategoryId && x.DomainId == domainId, ct) ?? throw new ValidationException("Category is not in the selected domain.");
        row.Name = request.Name; row.Description = request.Description; row.AuthorName = request.AuthorName; row.Ingredients = request.Ingredients; row.Instructions = request.Instructions; row.Servings = request.Servings; row.IsApproved = request.IsApproved; row.RecipeCategory = category;
        await db.SaveChangesAsync(ct); return MapMaintenanceRecipe(row);
    }
    public async Task<bool?> DeleteAsync(int id, int domainId, int version, CancellationToken ct)
    { var row = await db.Recipe.FirstOrDefaultAsync(x => x.Id == id && x.DomainId == domainId, ct); if (row is null) return null; if (row.Version != version) throw new DbUpdateConcurrencyException(); db.Remove(row); await db.SaveChangesAsync(ct); return true; }
    public async Task<RecipeMaintenanceCategory> CreateCategoryAsync(RecipeCategoryWriteRequest request, int domainId, CancellationToken ct)
    { var row = new RecipeCategory { Name = request.Name, Comment = request.Description, DisplayOrder = request.DisplayOrder, IsActive = request.IsActive, DomainId = domainId }; db.RecipeCategory.Add(row); await db.SaveChangesAsync(ct); return MapMaintenanceCategory(row); }
    public async Task<RecipeMaintenanceCategory?> UpdateCategoryAsync(int id, RecipeCategoryWriteRequest request, int domainId, int version, CancellationToken ct)
    { var row = await db.RecipeCategory.FirstOrDefaultAsync(x => x.Id == id && x.DomainId == domainId, ct); if (row is null) return null; if (row.Version != version) throw new DbUpdateConcurrencyException(); row.Name = request.Name; row.Comment = request.Description; row.DisplayOrder = request.DisplayOrder; row.IsActive = request.IsActive; await db.SaveChangesAsync(ct); return MapMaintenanceCategory(row); }
    public async Task<bool?> DeleteCategoryAsync(int id, int domainId, int version, CancellationToken ct)
    { var row = await db.RecipeCategory.FirstOrDefaultAsync(x => x.Id == id && x.DomainId == domainId, ct); if (row is null) return null; if (row.Version != version) throw new DbUpdateConcurrencyException(); if (await db.Recipe.AnyAsync(x => x.DomainId == domainId && x.RecipeCategory.Id == id, ct)) throw new InvalidOperationException("Category in use."); db.Remove(row); await db.SaveChangesAsync(ct); return true; }
    private static RecipeModel Map(global::MakeBoldSpark.Recipe.Data.Recipe x) => new() { Id = x.Id, Name = x.Name, Description = x.Description, AuthorNM = x.AuthorName, Ingredients = x.Ingredients, Instructions = x.Instructions, Servings = x.Servings, IsApproved = x.IsApproved, DomainID = x.DomainId ?? 0, RecipeCategoryID = x.RecipeCategory?.Id ?? 0, RecipeCategoryNM = x.RecipeCategory?.Name ?? "", ModifiedDT = x.UpdatedDate, Version = x.Version };
    private static RecipeCategoryModel Map(RecipeCategory x) => new() { Id = x.Id, Name = x.Name, Description = x.Comment, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive, DomainID = x.DomainId ?? 0, Version = x.Version };
    private static RecipeMaintenanceRecipe MapMaintenanceRecipe(global::MakeBoldSpark.Recipe.Data.Recipe x) => new(x.Id, x.Name, x.Description, x.AuthorName, x.Ingredients, x.Instructions, x.Servings, x.IsApproved, x.DomainId ?? 0, x.RecipeCategory?.Id ?? 0, x.RecipeCategory?.Name ?? "", x.Version, x.UpdatedDate);
    private static RecipeMaintenanceCategory MapMaintenanceCategory(RecipeCategory x) => new(x.Id, x.Name, x.Comment, x.DisplayOrder, x.IsActive, x.DomainId ?? 0, x.Version);
}
