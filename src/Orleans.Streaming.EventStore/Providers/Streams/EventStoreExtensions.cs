using System.Text.RegularExpressions;
using EventStore.Client;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
/// </summary>
public static class EventStoreExtensions
{
    private static readonly Regex s_PositionRegex = new(@"C:(\d+)/P:(\d+)", RegexOptions.Compiled);

    /// <summary>
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
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
        return Position.Start;
    }
}
