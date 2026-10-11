namespace MakeBoldSpark.Api.Features.FamilyMemories;

public static class FamilyMemoryEndpoints
{
    public const string CorsPolicy = "FamilyMemoriesPublic";

    public static IServiceCollection AddFamilyMemories(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<FamilyMemoryRepository>();
        services.AddScoped<FamilyMemoryService>();
        services.AddCors(options =>
        {
            var developmentOrigins = configuration.GetSection("FamilyMemories:DevelopmentOrigins").Get<string[]>() ?? [];
            foreach (var origin in developmentOrigins)
            {
                if (origin.Contains('*') || !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                    || (uri.Scheme != "http" && uri.Scheme != "https")
                    || origin != uri.GetLeftPart(UriPartial.Authority) || !string.IsNullOrEmpty(uri.UserInfo))
                    throw new InvalidOperationException("FamilyMemories:DevelopmentOrigins must contain explicit HTTP(S) origins without paths or wildcards.");
            }
            options.AddPolicy(CorsPolicy, policy => policy
                .WithOrigins(["https://texecon.com", .. developmentOrigins])
                .WithMethods("GET").WithHeaders("Accept"));
        });
        return services;
    }

    public static void MapFamilyMemories(this WebApplication app)
    {
        var publicGroup = app.MapGroup("/api/public/family-memories")
            .WithTags("Family Memories").AddEndpointFilter<FamilyMemoryResponseFilter>();
        publicGroup.MapGet("", async (FamilyMemoryService service, CancellationToken ct) =>
            Results.Ok(await service.PublishedAsync(ct)))
            .AllowAnonymous().RequireCors(CorsPolicy).WithName("GetPublishedFamilyMemories")
            .WithSummary("Entire published collection in editorial order; raw text, no pagination.")
            .Produces<PublishedMemoriesResponse>().ProducesProblem(500);

        var admin = app.MapGroup("/api/admin/makeboldspark/family-memories")
            .RequireAuthorization("AdminOnly").WithTags("Family Memories Admin")
            .AddEndpointFilter<FamilyMemoryResponseFilter>();
        admin.MapGet("", async (FamilyMemoryService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
            .WithName("ListFamilyMemories").Produces<List<FamilyMemory>>().Produces(401).Produces(403).ProducesProblem(500);
        admin.MapGet("/{id:int}", async (int id, FamilyMemoryService service, CancellationToken ct) =>
        {
            var memory = await service.FindAsync(id, ct);
            return memory is null ? Results.NotFound() : Results.Ok(memory);
        }).WithName("GetFamilyMemory").Produces<FamilyMemory>().Produces(404).Produces(401).Produces(403).ProducesProblem(500);
        admin.MapPost("", async (FamilyMemoryWrite input, FamilyMemoryService service, CancellationToken ct) =>
        {
            var errors = input.Validate();
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var memory = await service.CreateAsync(input, ct);
            return Results.Created($"/api/admin/makeboldspark/family-memories/{memory.Id}", memory);
        }).WithName("CreateFamilyMemory").Produces<FamilyMemory>(201).ProducesValidationProblem()
            .Produces(401).Produces(403).ProducesProblem(500);
        admin.MapPut("/{id:int}", async (int id, FamilyMemoryWrite input, FamilyMemoryService service, CancellationToken ct) =>
        {
            var errors = input.Validate();
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var memory = await service.UpdateAsync(id, input, ct);
            return memory is null ? Results.NotFound() : Results.Ok(memory);
        }).WithName("UpdateFamilyMemory").WithSummary("Replace editable fields; publish, unpublish or reorder.")
            .Produces<FamilyMemory>().ProducesValidationProblem().Produces(404).Produces(401).Produces(403).ProducesProblem(500);
        admin.MapDelete("/{id:int}", async (int id, FamilyMemoryService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteFamilyMemory").Produces(204).Produces(404).Produces(401).Produces(403).ProducesProblem(500);
    }
}

public class FamilyMemoryResponseFilter(ILogger<FamilyMemoryResponseFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        try { return await next(context); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log type and correlation, never submitted text or database exception details.
            logger.LogError("Family memories operation failed ({ExceptionType}); TraceId={TraceId}",
                ex.GetType().Name, context.HttpContext.TraceIdentifier);
            return Results.Problem(statusCode: 500, title: "Unable to complete the family memories request.");
        }
    }
}
