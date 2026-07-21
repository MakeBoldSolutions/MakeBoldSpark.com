using MakeBoldSpark.Api.Features.Bold;
using MakeBoldSpark.Api.Infrastructure.OpenApi;

namespace MakeBoldSpark.Api.Features.Bold.Embeddings;

/// <summary>
/// Embeddings execution is Out of scope for this milestone (spec.md: Phase 4 reference packs).
/// The endpoint exists so the contract needs no breaking change later — it always returns a
/// stable not_implemented error.
/// </summary>
public static class EmbeddingsEndpoints
{
    public static RouteGroupBuilder MapBoldEmbeddingsApi(this RouteGroupBuilder group)
    {
        group.MapPost("/embeddings", () =>
                BoldErrors.NotImplemented("not_implemented", "Embeddings execution is not implemented in this milestone (Phase 4)."))
            .WithName("PostBoldEmbeddings")
            .WithTags(MakeBoldSparkOpenApiTags.BoldEmbeddings)
            .WithSummary("Execute an embedding request (stub)")
            .WithDescription("Always returns 501 with code 'not_implemented'/retryable:false. Reserved for Phase 4 reference-pack retrieval.")
            .Produces<ErrorResponseDto>(StatusCodes.Status501NotImplemented)
            .Produces<ErrorResponseDto>(StatusCodes.Status401Unauthorized);

        return group;
    }
}
