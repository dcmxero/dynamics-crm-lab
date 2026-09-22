using System.Text;
using System.Text.Json;

namespace DynamicsCrmLab.Infrastructure.Dataverse;

/// <summary>
/// Carries where a listing left off, in a form a caller can hand back.
/// </summary>
/// <remarks>
/// Dataverse resumes a listing from a page number and a paging cookie, and the
/// cookie is XML. Neither belongs in a query string on its own: one is an
/// implementation detail a client could start inventing values for, the other
/// needs escaping. Both travel as one opaque string instead, which also means
/// the pair can never be separated.
/// </remarks>
internal static class Cursor
{
    /// <summary>
    /// Reads a cursor handed back by a caller.
    /// </summary>
    /// <remarks>
    /// A cursor that cannot be read starts the listing again rather than
    /// failing: it is a stale or mangled token, not a broken request, and the
    /// first page is a truthful answer to it.
    /// </remarks>
    /// <param name="cursor">The cursor, or <see langword="null"/> to start at the beginning.</param>
    /// <returns>Where to resume.</returns>
    public static (int PageNumber, string? Cookie) Read(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return (1, null);
        }

        try
        {
            var json = Encoding.UTF8.GetString(FromBase64Url(cursor));
            var position = JsonSerializer.Deserialize<Position>(json);

            return position is { PageNumber: > 0 } ? (position.PageNumber, position.Cookie) : (1, null);
        }
        catch (Exception exception) when (exception is FormatException or JsonException or DecoderFallbackException)
        {
            return (1, null);
        }
    }

    /// <summary>
    /// Writes the cursor that resumes after the page just read.
    /// </summary>
    /// <param name="pageNumber">The page to ask for next.</param>
    /// <param name="cookie">The paging cookie the platform returned.</param>
    /// <returns>An opaque cursor safe to put in a query string.</returns>
    public static string Write(int pageNumber, string? cookie) =>
        ToBase64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Position(pageNumber, cookie))));

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');

        return Convert.FromBase64String(padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '='));
    }

    private sealed record Position(int PageNumber, string? Cookie);
}
