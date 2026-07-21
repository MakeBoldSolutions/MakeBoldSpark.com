using Microsoft.EntityFrameworkCore;
using MakeBoldSpark.Recipe.Constants;
using MakeBoldSpark.Recipe.Data;
using MakeBoldSpark.Recipe.Helpers;
using MakeBoldSpark.Recipe.Interfaces;
using MakeBoldSpark.Recipe.Models;

namespace MakeBoldSpark.Recipe.Providers;

/// <summary>
/// Recipe Service implementation. Implements IRecipeService only.
/// IMenuProvider has been removed — portal callers use RecipeMenuAdapter in MakeBoldSpark.Portal.
/// </summary>
public class RecipeProvider(RecipeDbContext dbContext) : IRecipeService, IDisposable
{
    private bool disposedValue;

    private List<RecipeModel> Create(List<RecipeEntity>? list)
    {
        if (list == null) return [];
        return [.. list.Select(Create).OrderBy(x => x.Name)];
    }

    private List<RecipeCategoryModel> Create(List<RecipeCategory>? list)
    {
        if (list == null) return [];
        return [.. list.Select(item => Create(item)).OrderBy(x => x.Name)];
    }

    private RecipeModel Create(RecipeEntity? recipe)
    {
        if (recipe == null) return new RecipeModel();
        return new RecipeModel()
        {
            DomainID = recipe.DomainId ?? RecipeConstants.INT_MOM_DomainId,
            RecipeURL = RecipeUrlHelper.GetRecipeURL(recipe.Name),
            Id = recipe.Id,
            Name = recipe.Name,
            Ingredients = recipe.Ingredients,
            Instructions = recipe.Instructions,
            Description = string.IsNullOrEmpty(recipe.Description) ? recipe.Name : recipe.Description,
            SEO_Keywords = recipe.Keywords,
            Servings = recipe.Servings,
            AuthorNM = recipe.AuthorName,
            AverageRating = recipe.AverageRating,
            IsApproved = recipe.IsApproved,
            CommentCount = 0,
            RecipeCategory = Create(recipe.RecipeCategory),
            RecipeCategoryID = recipe.RecipeCategory?.Id ?? 0,
            RatingCount = recipe.RatingCount,
            ViewCount = recipe.ViewCount,
            LastViewDT = recipe.LastViewDt,
            ModifiedDT = recipe.UpdatedDate,
            Version = recipe.Version,
        };
    }

    private RecipeEntity Create(RecipeModel? recipe)
    {
        if (recipe == null) return new RecipeEntity();
        if (recipe.DomainID == 0) recipe.DomainID = RecipeConstants.INT_MOM_DomainId;

        var category = dbContext.RecipeCategory.FirstOrDefault(w => w.Id == recipe.RecipeCategoryID);

        return new RecipeEntity()
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Ingredients = recipe.Ingredients,
            Instructions = recipe.Instructions,
            Keywords = recipe.SEO_Keywords,
            Description = string.IsNullOrEmpty(recipe.Description) ? recipe.Name : recipe.Description,
            AuthorName = recipe.AuthorNM,
            AverageRating = recipe.AverageRating,
            IsApproved = recipe.IsApproved,
            CommentCount = recipe.CommentCount,
            RatingCount = recipe.RatingCount,
            ViewCount = recipe.ViewCount,
            LastViewDt = recipe.LastViewDT,
            DomainId = recipe.DomainID,
            RecipeCategory = category ?? new RecipeCategory { Name = recipe.Name, Comment = recipe.Name },
            Servings = recipe.Servings,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Version = recipe.Version,
        };
    }

    private static RecipeCategory Create(RecipeCategoryModel? s)
    {
        if (s == null) return new RecipeCategory();
        return new RecipeCategory()
        {
            DisplayOrder = s.DisplayOrder,
            IsActive = s.IsActive,
            Comment = s.Name,
            Id = s.Id,
            Name = s.Name,
            DomainId = s.DomainID,
            Version = s.Version
        };
    }

    private List<RecipeImageModel> Create(List<RecipeImage>? list)
    {
        var result = new List<RecipeImageModel>();
        if (list == null) return result;
        foreach (var item in list)
        {
            if (item == null) continue;
            result.Add(Create(item));
        }
        return result;
    }

    private RecipeImageModel Create(RecipeImage? item)
    {
        if (item == null) return new RecipeImageModel();
        return new RecipeImageModel()
        {
            Id = item.Id,
            Recipe = Create(item.Recipe),
            DisplayOrder = item.DisplayOrder,
            FileDescription = item.FileDescription,
            FileName = item.FileName,
        };
    }

    private RecipeCategoryModel Create(RecipeCategory? rc, bool loadRecipes = false)
    {
        if (rc == null) return new RecipeCategoryModel();
        return new RecipeCategoryModel()
        {
            DomainID = rc.DomainId ?? RecipeConstants.INT_MOM_DomainId,
            DisplayOrder = rc.DisplayOrder,
            IsActive = rc.IsActive,
            Description = rc.Comment,
            Id = rc.Id,
            Name = rc.Name,
            Url = RecipeUrlHelper.GetRecipeCategoryURL(rc.Name),
            Version = rc.Version,
            Recipes = loadRecipes ? Create(rc.Recipe?.ToList()) : []
        };
    }

    public bool Delete(int Id)
    {
        var item = dbContext.Recipe.FirstOrDefault(w => w.Id == Id);
        if (item != null)
        {
            dbContext.Recipe.Remove(item);
            dbContext.SaveChanges();
            return true;
        }
        return false;
    }

    public bool Delete(RecipeCategoryModel saveItem)
    {
        var item = dbContext.RecipeCategory.FirstOrDefault(w => w.Id == saveItem.Id);
        if (item != null)
        {
            dbContext.RecipeCategory.Remove(item);
            dbContext.SaveChanges();
            return true;
        }
        return false;
    }

    public IEnumerable<RecipeModel> Get()
    {
        var list = dbContext.Recipe
            .AsNoTracking()
            .Include(r => r.RecipeCategory)
            .Include(i => i.RecipeImage)
            .ToList();
        return Create(list);
    }

    public RecipeModel Get(int Id)
    {
        var recipe = Create(dbContext.Recipe
            .AsNoTracking()
            .Where(w => w.Id == Id)
            .Include(r => r.RecipeCategory)
            .FirstOrDefault());
        recipe.RecipeCategories = dbContext.RecipeCategory
            .AsNoTracking()
            .Select(s => new RecipeOptionModel { Value = s.Id.ToString(), Text = s.Name })
            .ToList();
        return recipe;
    }

    public RecipeCategoryModel GetRecipeCategoryById(int Id)
    {
        return Create(dbContext.RecipeCategory
            .AsNoTracking()
            .Include(i => i.Recipe)
            .FirstOrDefault(w => w.Id == Id), loadRecipes: true);
    }

    public List<RecipeCategoryModel> GetRecipeCategoryList()
    {
        return Create(dbContext.RecipeCategory.AsNoTracking().ToList());
    }

    public List<RecipeImageModel> GetRecipeImages()
    {
        return Create(dbContext.RecipeImage.Include(i => i.Recipe).ToList());
    }

    public RecipeModel Save(RecipeModel? saveItem)
    {
        if (saveItem == null) return new RecipeModel();
        if (saveItem.Id == 0)
        {
            var entity = Create(saveItem);
            dbContext.Recipe.Add(entity);
            dbContext.SaveChanges();
            saveItem.Id = entity.Id;
        }
        else
        {
            var entity = dbContext.Recipe
                .Where(w => w.Id == saveItem.Id)
                .Include(i => i.RecipeCategory)
                .FirstOrDefault();
            if (entity != null)
            {
                if (entity.RecipeCategory == null || entity.RecipeCategory.Id != saveItem.RecipeCategoryID)
                {
                    entity.RecipeCategory = (dbContext.RecipeCategory.FirstOrDefault(w => w.Id == saveItem.RecipeCategoryID) ?? entity.RecipeCategory)!;
                }
                entity.Name = saveItem.Name;
                entity.AuthorName = saveItem.AuthorNM;
                entity.Description = saveItem.Description;
                entity.Ingredients = saveItem.Ingredients;
                entity.Instructions = saveItem.Instructions;
                entity.Servings = saveItem.Servings;
                entity.IsApproved = saveItem.IsApproved;
                dbContext.SaveChanges();
            }
        }
        return Get(saveItem.Id);
    }

    public IEnumerable<RecipeModel> Save(List<RecipeModel>? saveRecipes)
    {
        if (saveRecipes == null) return [];
        var current = Get();
        var categories = GetRecipeCategoryList();
        foreach (var item in saveRecipes)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.RecipeCategoryNM)) continue;
            item.Id = current.FirstOrDefault(w => w.Name == item.Name)?.Id ?? 0;
            item.RecipeCategoryID = categories.FirstOrDefault(w => w.Name == item.RecipeCategoryNM)?.Id ?? 0;
            if (item.Id == 0)
            {
                var entity = Create(item);
                entity.UpdatedDate = DateTime.UtcNow;
                dbContext.Recipe.Add(entity);
                dbContext.SaveChanges();
                item.Id = entity.Id;
            }
            else
            {
                var entity = dbContext.Recipe.FirstOrDefault(w => w.Id == item.Id);
                if (entity != null) dbContext.SaveChanges();
            }
        }
        return Get();
    }

    public RecipeCategoryModel Save(RecipeCategoryModel saveItem)
    {
        if (saveItem == null) return new RecipeCategoryModel();
        if (saveItem.Id == 0)
        {
            var entity = Create(saveItem);
            dbContext.RecipeCategory.Add(entity);
            dbContext.SaveChanges();
            saveItem.Id = entity.Id;
        }
        else
        {
            var entity = dbContext.RecipeCategory.FirstOrDefault(w => w.Id == saveItem.Id);
            if (entity != null)
            {
                entity.Name = saveItem.Name;
                entity.Comment = saveItem.Description;
                entity.DisplayOrder = saveItem.DisplayOrder;
                entity.IsActive = saveItem.IsActive;
                dbContext.SaveChanges();
            }
        }
        return GetRecipeCategoryById(saveItem.Id);
    }

    public List<RecipeCategoryModel> Save(List<RecipeCategoryModel>? saveCategories)
    {
        if (saveCategories == null) return [];
        var current = GetRecipeCategoryList();
        foreach (var item in saveCategories)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name)) continue;
            item.Id = current.FirstOrDefault(w => w.Name == item.Name)?.Id ?? 0;
            if (item.Id == 0)
            {
                var entity = Create(item);
                entity.UpdatedDate = DateTime.UtcNow;
                dbContext.RecipeCategory.Add(entity);
                dbContext.SaveChanges();
                item.Id = entity.Id;
            }
            else
            {
                var entity = dbContext.RecipeCategory.FirstOrDefault(w => w.Id == item.Id);
                if (entity != null)
                {
                    entity.Name = item.Name;
                    entity.Comment = item.Description;
                    entity.UpdatedDate = DateTime.UtcNow;
                    dbContext.SaveChanges();
                }
            }
        }
        return GetRecipeCategoryList();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing) dbContext?.Dispose();
            disposedValue = true;
        }
    }

    ~RecipeProvider() => Dispose(disposing: false);

    void IDisposable.Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
