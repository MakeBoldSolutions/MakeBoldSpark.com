using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MakeBoldSpark.Api.Features.ArrowSparkReactions;

public enum ArrowSparkStoreOutcome { Stored, CeilingReached }

/// <summary>
/// Insert-only storage: one <c>&lt;recordId&gt;.json</c> per reaction, written to a temp name in the
/// same directory and renamed into place (API-60, API-63). Nothing here ever reads a record back.
/// </summary>
public sealed class ArrowSparkReactionStore(IOptionsMonitor<ArrowSparkReactionOptions> options)
{
    public const string TempPrefix = ".tmp-";

    public async Task<ArrowSparkStoreOutcome> SaveAsync(string reactionType, byte[] canonicalPayload, CancellationToken ct)
    {
        var settings = options.CurrentValue;
        var directory = settings.StoragePath;
        // Fail closed: a missing or unconfigured directory is a 5xx, never a silent drop (API-110).
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            throw new DirectoryNotFoundException("ArrowSpark reaction storage is not available.");

        if (Directory.EnumerateFiles(directory, "*.json").Skip(settings.MaxFiles - 1).Any())
            return ArrowSparkStoreOutcome.CeilingReached;

        var recordId = Guid.NewGuid();
        var received = DateTimeOffset.UtcNow;
        // Truncated to the minute: the only time data stored (API-61, API-62).
        var receivedUtc = received.ToString("yyyy-MM-dd'T'HH:mm'Z'", System.Globalization.CultureInfo.InvariantCulture);

        var tempPath = Path.Combine(directory, TempPrefix + Guid.NewGuid().ToString("N"));
        var finalPath = Path.Combine(directory, recordId.ToString("D") + ".json");
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();
                    writer.WriteString("recordId", recordId);
                    writer.WriteNumber("schemaVersion", 1);
                    writer.WriteString("receivedUtc", receivedUtc);
                    writer.WriteString("reactionType", reactionType);
                    writer.WritePropertyName("payload");
                    writer.WriteRawValue(canonicalPayload, skipInputValidation: true);
                    writer.WriteEndObject();
                }
                await stream.FlushAsync(ct);
                stream.Flush(flushToDisk: true);
            }
            File.Move(tempPath, finalPath, overwrite: false);
        }
        catch
        {
            try { File.Delete(tempPath); } catch { /* best effort; startup cleanup removes leftovers */ }
            throw;
        }
        return ArrowSparkStoreOutcome.Stored;
    }
}

/// <summary>Removes <c>.tmp-*</c> leftovers from a crash mid-write (API-63).</summary>
public sealed class ArrowSparkTempCleanupService(
    IOptions<ArrowSparkReactionOptions> options,
    ILogger<ArrowSparkTempCleanupService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var directory = options.Value.StoragePath;
        try
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                logger.LogWarning("ArrowSparkReactions:StoragePath is not configured or does not exist; the endpoint will fail closed.");
                return Task.CompletedTask;
            }
            foreach (var file in Directory.EnumerateFiles(directory, ArrowSparkReactionStore.TempPrefix + "*"))
                File.Delete(file);
        }
        catch (Exception ex)
        {
            logger.LogWarning("ArrowSpark temp cleanup failed ({ExceptionType}).", ex.GetType().Name);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
