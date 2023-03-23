using System.Text.RegularExpressions;
using EventStore.Client;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
/// </summary>
public static class EventStoreExtensions
{
    private static readonly Regex s_PositionRegex = new(@"C:(\d+)/P:(\d+)", RegexOptions.Compiled);

    /// <summary>
    ///     Converts a string in the format of "commitPosition:preparePosition" to a <see cref="Position" /> object.
    /// </summary>
    /// <param name="source">The source string in the format of "commitPosition:preparePosition".</param>
    /// <returns>The <see cref="Position" /> object converted from the source string, or <see cref="Position.Start" /> if the conversion fails.</returns>
    public static Position ToPosition(this string source)
    {
        var match = s_PositionRegex.Match(source);
        if (match.Success)
        {
            if (ulong.TryParse(match.Groups[1].Value, out var commitPosition) && ulong.TryParse(match.Groups[2].Value, out var preparePosition))
            {
                return new Position(commitPosition, preparePosition);
            }
        }
        return default;
    }

    /// <summary>
    ///     Tries to convert a string in the format of "commitPosition:preparePosition" to a <see cref="Position" /> object.
    /// </summary>
    /// <param name="source">The source string in the format of "commitPosition:preparePosition".</param>
    /// <param name="position">The converted <see cref="Position" /> object, or <see cref="Position.Start" /> if the conversion fails.</param>
    /// <returns>True if the conversion is successful, false otherwise.</returns>
    public static bool TryToPosition(this string source, out Position position)
    {
        var match = s_PositionRegex.Match(source);
        if (match.Success)
        {
            if (ulong.TryParse(match.Groups[1].Value, out var commitPosition) && ulong.TryParse(match.Groups[2].Value, out var preparePosition))
            {
                position = new Position(commitPosition, preparePosition);
                return true;
            }
        }
        position = default;
        return false;
    }

    /// <summary>
    ///     Converts a string to a <see cref="StreamPosition" /> object.
    /// </summary>
    /// <param name="source">The string to convert.</param>
    /// <returns>The resulting <see cref="StreamPosition" /> object.</returns>
    public static StreamPosition ToStreamPosition(this string source)
    {
        if (ulong.TryParse(source, out var streamPosition))
        {
            return new StreamPosition(streamPosition);
        }
        return default;
    }

    /// <summary>
    ///     Tries to convert a string to a <see cref="StreamPosition" /> object.
    /// </summary>
    /// <param name="source">The string to convert.</param>
    /// <param name="position">The resulting <see cref="StreamPosition" /> object, if the conversion succeeded.</param>
    /// <returns>True if the conversion succeeded, false otherwise.</returns>
    public static bool TryToStreamPosition(this string source, out StreamPosition position)
    {
        if (ulong.TryParse(source, out var streamPosition))
        {
            position = new StreamPosition(streamPosition);
            return true;
        }
        position = default;
        return false;
    }

}
