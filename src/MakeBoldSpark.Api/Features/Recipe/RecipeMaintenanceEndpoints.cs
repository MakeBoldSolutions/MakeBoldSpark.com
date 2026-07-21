using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using MakeBoldSpark.Api.Infrastructure.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakeBoldSpark.Api.Features.Recipe;

public static class RecipeMaintenanceEndpoints
{
    private const long MaxMutationBodyBytes = 256 * 1024;
    private static readonly Meter Meter = new("MakeBoldSpark.Api.RecipeMaintenance");
    private static readonly Counter<long> OperationCount = Meter.CreateCounter<long>("recipe_publisher_operations_total");
    private static readonly Counter<long> ErrorCount = Meter.CreateCounter<long>("recipe_publisher_operation_errors_total");
    private static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>("recipe_publisher_operation_duration_ms");

    public static RouteGroupBuilder MapPublisherRecipeMaintenanceApi(this RouteGroupBuilder group)
    {
        var api = group.MapGroup("/recipes")
            .WithTags(MakeBoldSparkOpenApiTags.RecipesPublishing);
        api.AddEndpointFilter(ObservePublisherOperationAsync);

        api.MapGet("/domains", async (RecipeService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetDomainsAsync(ct)))
            .WithName("ListPublisherRecipeDomains")
            .Produces<IReadOnlyList<MaintenanceDomain>>();

        api.MapGet("", async ([FromQuery] int? domainId, [FromQuery] int? page, [FromQuery] int? pageSize, RecipeService svc, CancellationToken ct) =>
        {
            if (!TryGetDomainId(domainId, out var selectedDomain, out var error))
                return error;

            var selectedPage = Math.Max(1, page ?? 1);
            var selectedPageSize = Math.Clamp(pageSize ?? 50, 1, 100);
            return Results.Ok(await svc.ListAsync(selectedDomain, selectedPage, selectedPageSize, ct));
        })
        .WithName("ListPublisherRecipes")
        .Produces<PagedResult<RecipeMaintenanceRecipe>>()
        .ProducesValidationProblem();

        api.MapGet("/{id:int}", async (int id, [FromQuery] int? domainId, RecipeService svc, CancellationToken ct) =>
        {
            if (!TryGetDomainId(domainId, out var selectedDomain, out var error))
                return error;

            return await svc.GetAsync(id, selectedDomain, ct) is { } item
                ? Results.Ok(item)
                : Results.NotFound();
        })
        .WithName("GetPublisherRecipe")
        .Produces<RecipeMaintenanceRecipe>()
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        api.MapPost("", async (RecipeWriteRequest request, [FromQuery] int? domainId, HttpRequest http, RecipeService svc, CancellationToken ct) =>
        {
            if (!TryValidateMutation(http, domainId, request, out var selectedDomain, out var error))
                return error;

            try
            {
                var created = await svc.CreateAsync(request, selectedDomain, ct);
                return Results.Created($"/api/publish/recipes/{created.Id}?domainId={selectedDomain}", created);
            }
            catch (ValidationException ex)
            {
                return ValidationProblem(ex.Message);
            }
        })
        .WithName("CreatePublisherRecipe")
        .Produces<RecipeMaintenanceRecipe>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status413PayloadTooLarge)
        .RequireRateLimiting("publisher-recipe-mutation");

        api.MapPut("/{id:int}", async (int id, RecipeWriteRequest request, [FromQuery] int? domainId, [FromHeader(Name = "If-Match")] string? ifMatch, HttpRequest http, RecipeService svc, CancellationToken ct) =>
        {
            if (!TryValidateMutation(http, domainId, request, out var selectedDomain, out var error))
                return error;
            if (!TryGetVersion(ifMatch, out var version, out error))
                return error;

            try
            {
                return await svc.UpdateAsync(id, request, selectedDomain, version, ct) is { } item
                    ? Results.Ok(item)
                    : Results.NotFound();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            catch (ValidationException ex)
            {
                return ValidationProblem(ex.Message);
            }
        })
        .WithName("UpdatePublisherRecipe")
        .Produces<RecipeMaintenanceRecipe>()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status412PreconditionFailed)
        .Produces(StatusCodes.Status413PayloadTooLarge)
        .RequireRateLimiting("publisher-recipe-mutation");

        api.MapDelete("/{id:int}", async (int id, [FromQuery] int? domainId, [FromHeader(Name = "If-Match")] string? ifMatch, RecipeService svc, CancellationToken ct) =>
        {
            if (!TryGetDomainId(domainId, out var selectedDomain, out var error))
                return error;
            if (!TryGetVersion(ifMatch, out var version, out error))
                return error;

            try
            {
                return await svc.DeleteAsync(id, selectedDomain, version, ct) is null
                    ? Results.NotFound()
                    : Results.NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.StatusCode(StatusCodes.Status412PreconditionFailed);
            }
        })
        .WithName("DeletePublisherRecipe")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status412PreconditionFailed)
        .RequireRateLimiting("publisher-recipe-mutation");

        api.MapGet("/categories", async ([FromQuery] int? domainId, RecipeService svc, CancellationToken ct) =>
        {
            if (!TryGetDomainId(domainId, out var selectedDomain, out var error))
                return error;

            return Results.Ok(await svc.CategoriesAsync(selectedDomain, ct));
        })
        .WithName("ListPublisherRecipeCategories")
        .Produces<IReadOnlyList<RecipeMaintenanceCategory>>()
        .ProducesValidationProblem();

        api.MapGet("/categories/{id:int}", async (int id, [FromQuery] int? domainId, RecipeService svc, CancellationToken ct) =>
        {
            if (!TryGetDomainId(domainId, out var selectedDomain, out var error))
                return error;

            return await svc.GetCategoryAsync(id, selectedDomain, ct) is { } item
                ? Results.Ok(item)
                : Results.NotFound();
        })
        .WithName("GetPublisherRecipeCategory")
        .Produces<RecipeMaintenanceCategory>()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/categories", async (RecipeCategoryWriteRequest request, [FromQuery] int? domainId, HttpRequest http, RecipeService svc, CancellationToken ct) =>
        {
            if (!TryValidateMutation(http, domainId, request, out var selectedDomain, out var error))
                return error;

            var created = await svc.CreateCategoryAsync(request, selectedDomain, ct);
            return Results.Created($"/api/publish/recipes/categories/{created.Id}?domainId={selectedDomain}", created);
        })
        .WithName("CreatePublisherRecipeCategory")
        .Produces<RecipeMaintenanceCategory>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status413PayloadTooLarge)
        .RequireRateLimiting("publisher-recipe-mutation");

        api.MapPut("/categories/{id:int}", async (int id, RecipeCategoryWriteRequest request, [FromQuery] int? domainId, [FromHeader(Name = "If-Match")] string? ifMatch, HttpRequest http, RecipeService svc, CancellationToken ct) =>
        {
            if (!TryValidateMutation(http, domainId, request, out var selectedDomain, out var error))
                return error;
            if (!TryGetVersion(ifMatch, out var version, out error))
                return error;

            try
            {
                return await svc.UpdateCategoryAsync(id, request, selectedDomain, version, ct) is { } item
                    ? Results.Ok(item)
                    : Results.NotFound();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.StatusCode(StatusCodes.Status412PreconditionFailed);
            }
        })
        .WithName("UpdatePublisherRecipeCategory")
        .Produces<RecipeMaintenanceCategory>()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status412PreconditionFailed)
        .Produces(StatusCodes.Status413PayloadTooLarge)
        .RequireRateLimiting("publisher-recipe-mutation");

        api.MapDelete("/categories/{id:int}", async (int id, [FromQuery] int? domainId, [FromHeader(Name = "If-Match")] string? ifMatch, RecipeService svc, CancellationToken ct) =>
        {
            if (!TryGetDomainId(domainId, out var selectedDomain, out var error))
                return error;
            if (!TryGetVersion(ifMatch, out var version, out error))
                return error;

            try
            {
                return await svc.DeleteCategoryAsync(id, selectedDomain, version, ct) is null
                    ? Results.NotFound()
                    : Results.NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.StatusCode(StatusCodes.Status412PreconditionFailed);
            }
            catch (InvalidOperationException)
            {
                return Results.Conflict(new { message = "Category is in use." });
            }
        })
        .WithName("DeletePublisherRecipeCategory")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status412PreconditionFailed)
        .RequireRateLimiting("publisher-recipe-mutation");

        return group;
    }

    private static async ValueTask<object?> ObservePublisherOperationAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var operation = http.GetEndpoint()?.DisplayName ?? $"{http.Request.Method} {http.Request.Path}";
        var logger = http.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("MakeBoldSpark.Api.Features.Recipe.RecipeMaintenance");
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await next(context);
            sw.Stop();
            var statusCode = result is IStatusCodeHttpResult statusResult && statusResult.StatusCode.HasValue
                ? statusResult.StatusCode.Value
                : StatusCodes.Status200OK;

            RecordOperation(operation, statusCode, sw.Elapsed.TotalMilliseconds);
            logger.LogInformation(
                "Publisher recipe operation {Operation} returned {StatusCode} in {ElapsedMilliseconds} ms",
                operation,
                statusCode,
                sw.Elapsed.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            RecordOperation(operation, StatusCodes.Status500InternalServerError, sw.Elapsed.TotalMilliseconds);
            ErrorCount.Add(1, Tags(operation, StatusCodes.Status500InternalServerError));
            logger.LogError(
                ex,
                "Publisher recipe operation {Operation} failed in {ElapsedMilliseconds} ms",
                operation,
                sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    private static void RecordOperation(string operation, int statusCode, double elapsedMs)
    {
        OperationCount.Add(1, Tags(operation, statusCode));
        OperationDuration.Record(elapsedMs, Tags(operation, statusCode));
        if (statusCode >= StatusCodes.Status400BadRequest)
            ErrorCount.Add(1, Tags(operation, statusCode));
    }

    private static KeyValuePair<string, object?>[] Tags(string operation, int statusCode) =>
    [
        new("operation", operation),
        new("status_code", statusCode)
    ];

    private static bool TryValidateMutation<T>(HttpRequest http, int? domainId, T request, out int selectedDomain, out IResult error)
    {
        selectedDomain = 0;
        if (http.ContentLength > MaxMutationBodyBytes)
        {
            error = Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            return false;
        }

        if (!TryGetDomainId(domainId, out selectedDomain, out error))
            return false;

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(request!);
        if (Validator.TryValidateObject(request!, context, validationResults, validateAllProperties: true))
        {
            error = Results.Empty;
            return true;
        }

        var errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty), (result, member) => new { result.ErrorMessage, member })
            .GroupBy(x => string.IsNullOrWhiteSpace(x.member) ? "request" : x.member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Invalid value.").ToArray());

        error = Results.ValidationProblem(errors);
        return false;
    }

    private static bool TryGetDomainId(int? domainId, out int selectedDomain, out IResult error)
    {
        selectedDomain = domainId ?? 0;
        if (selectedDomain > 0)
        {
            error = Results.Empty;
            return true;
        }

        error = ValidationProblem("A positive domainId query value is required.");
        return false;
    }

    private static bool TryGetVersion(string? ifMatch, out int version, out IResult error)
    {
        version = 0;
        var value = ifMatch?.Trim();
        if (value?.Length > 1 && value.StartsWith('"') && value.EndsWith('"'))
            value = value[1..^1];

        if (int.TryParse(value, out version) && version > 0)
        {
            error = Results.Empty;
            return true;
        }

        error = ValidationProblem("A strong If-Match header containing the current numeric version is required.");
        return false;
    }

    private static IResult ValidationProblem(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = [message]
        });
}
