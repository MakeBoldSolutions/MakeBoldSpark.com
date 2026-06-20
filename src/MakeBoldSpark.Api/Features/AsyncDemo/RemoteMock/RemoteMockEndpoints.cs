using MakeBoldSpark.Api.Features.AsyncDemo.Models;
using MakeBoldSpark.Api.Infrastructure.OpenApi;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MakeBoldSpark.Api.Features.AsyncDemo.RemoteMock;

public static class RemoteMockEndpoints
{
    public static RouteGroupBuilder MapRemoteMockApi(this RouteGroupBuilder group)
    {
        group.MapPost("/results", async (
            MockResultsRequest request,
            RemoteMockService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.RunMockAsync(request.LoopCount, request.MaxTimeMS, cancellationToken);

                if (result.ResultValue == "408")
                    return Results.Json(result, statusCode: 408);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        })
        .WithName("RemoteMockResults")
        .WithTags(MakeBoldSparkOpenApiTags.AsyncResilienceTimeouts)
        .WithSummary("Simulate a slow downstream server that may time out — used to demo retry patterns")
        .WithDescription("Runs a configurable number of loop iterations up to a maximum wall-clock time. Returns 408 when MaxTimeMS is exceeded so callers can observe timeout and retry behaviour without hitting a real external service.")
        .Produces<MockResults>(200)
        .Produces<MockResults>(408)
        .Produces(500)
        .AllowAnonymous();

        return group;
    }
}

/// <summary>Request parameters for the remote mock simulation.</summary>
public sealed class MockResultsRequest
{
    /// <summary>Number of loop iterations the mock service will attempt to execute.</summary>
    [Range(1, 10000)]
    [DefaultValue(250)]
    [Description("Number of loop iterations the mock service will attempt to execute.")]
    public int LoopCount { get; set; }
    /// <summary>Maximum allowed wall-clock time in milliseconds; the service returns 408 if this is exceeded.</summary>
    [Range(1, 120000)]
    [DefaultValue(2000)]
    [Description("Maximum allowed wall-clock time in milliseconds; the service returns 408 if this is exceeded.")]
    public int MaxTimeMS { get; set; }
}
