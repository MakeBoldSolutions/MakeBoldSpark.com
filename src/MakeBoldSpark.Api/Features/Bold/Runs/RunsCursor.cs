using System.Text;
using System.Text.Json;

namespace MakeBoldSpark.Api.Features.Bold.Runs;

/// <summary>
/// Opaque keyset-pagination cursor (spec.md AC6: clients pass it back unmodified and never parse
/// or construct it). Encodes the last page's row id — rows are insert-only and never reordered, so
/// the auto-increment id is a stable, monotonically-ordered key (and avoids SQLite's lack of
/// ORDER BY support for DateTimeOffset columns).
/// </summary>
public static class RunsCursor
{
    private record CursorPayload(int Id);

    public static string Encode(int id)
    {
        var json = JsonSerializer.Serialize(new CursorPayload(id));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static int? TryDecode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            return JsonSerializer.Deserialize<CursorPayload>(json)?.Id;
        }
        catch
        {
            return null;
        }
    }
}
