namespace MakeBoldSpark.Api.Features.Bold.Completions;

/// <summary>Reads at most the configured limit plus one byte, even without Content-Length.</summary>
public static class BoundedRequestBody
{
    public static async Task<byte[]?> ReadAsync(Stream stream, int limit, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);
        using var body = new MemoryStream();
        var buffer = new byte[(int)Math.Min(8192L, (long)limit + 1)];
        while (true)
        {
            var remaining = (int)Math.Min(buffer.Length, (long)limit - body.Length + 1);
            var count = await stream.ReadAsync(buffer.AsMemory(0, remaining), cancellationToken);
            if (count == 0) return body.ToArray();
            if (body.Length + count > limit) return null;
            body.Write(buffer, 0, count);
        }
    }
}
