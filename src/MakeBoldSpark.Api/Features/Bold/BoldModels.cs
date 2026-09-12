using System.Text.Json.Serialization;

namespace MakeBoldSpark.Api.Features.Bold;

/// <summary>Shared DTOs matching bold-api-openapi.json component schemas.</summary>
public record ErrorDetailDto(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("retryable")] bool Retryable);

public record ErrorResponseDto([property: JsonPropertyName("error")] ErrorDetailDto Error);

public static class BoldErrors
{
    public static IResult Unauthorized(string message = "Missing or invalid install token.")
        => Results.Json(new ErrorResponseDto(new ErrorDetailDto("unauthorized", message, false)), statusCode: StatusCodes.Status401Unauthorized);

    public static IResult BadRequest(string code, string message)
        => Results.Json(new ErrorResponseDto(new ErrorDetailDto(code, message, false)), statusCode: StatusCodes.Status400BadRequest);

    public static IResult NotFound(string code, string message)
        => Results.Json(new ErrorResponseDto(new ErrorDetailDto(code, message, false)), statusCode: StatusCodes.Status404NotFound);

    public static IResult UnprocessableEntity(string code, string message)
        => Results.Json(new ErrorResponseDto(new ErrorDetailDto(code, message, false)), statusCode: StatusCodes.Status422UnprocessableEntity);

    public static IResult BadGateway(string code, string message, bool retryable)
        => Results.Json(new ErrorResponseDto(new ErrorDetailDto(code, message, retryable)), statusCode: StatusCodes.Status502BadGateway);

    public static IResult TooManyRequests(string code, string message, int? retryAfterSeconds, bool retryable)
    {
        var result = Results.Json(new ErrorResponseDto(new ErrorDetailDto(code, message, retryable)), statusCode: StatusCodes.Status429TooManyRequests);
        if (retryAfterSeconds is int seconds)
        {
            return new RetryAfterResult(result, seconds);
        }
        return result;
    }

    public static IResult NotImplemented(string code, string message)
        => Results.Json(new ErrorResponseDto(new ErrorDetailDto(code, message, false)), statusCode: StatusCodes.Status501NotImplemented);
}

/// <summary>Wraps an IResult to add a Retry-After header (spec.md AC5 rate-limit/cost-cap responses).</summary>
public sealed class RetryAfterResult(IResult inner, int retryAfterSeconds) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        await inner.ExecuteAsync(httpContext);
    }
}
