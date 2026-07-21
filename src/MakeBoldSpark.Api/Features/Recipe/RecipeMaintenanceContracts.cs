using System.ComponentModel.DataAnnotations;

namespace MakeBoldSpark.Api.Features.Recipe;

public sealed class RecipeWriteRequest
{
    [Required, StringLength(150)] public string Name { get; init; } = "";
    [StringLength(500)] public string Description { get; init; } = "";
    [Required, StringLength(50)] public string AuthorName { get; init; } = "";
    [Required, StringLength(65536)] public string Ingredients { get; init; } = "";
    [Required, StringLength(65536)] public string Instructions { get; init; } = "";
    [Range(0, int.MaxValue)] public int Servings { get; init; }
    [Range(1, int.MaxValue)] public int RecipeCategoryId { get; init; }
    public bool IsApproved { get; init; }
}

public sealed class RecipeCategoryWriteRequest
{
    [Required, StringLength(70)] public string Name { get; init; } = "";
    [StringLength(1500)] public string Description { get; init; } = "";
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed record MaintenanceDomain(int Id, string Name);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record RecipeMaintenanceRecipe(
    int Id,
    string Name,
    string Description,
    string AuthorName,
    string Ingredients,
    string Instructions,
    int Servings,
    bool IsApproved,
    int DomainId,
    int RecipeCategoryId,
    string RecipeCategoryName,
    int Version,
    DateTime UpdatedDate);

public sealed record RecipeMaintenanceCategory(
    int Id,
    string Name,
    string Description,
    int DisplayOrder,
    bool IsActive,
    int DomainId,
    int Version);
