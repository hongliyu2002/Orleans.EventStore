using Orleans.FluentResults;

namespace SiloX.Domain.Abstractions;

/// <summary>
///     Represents a range of <see cref="DateTimeOffset" /> values.
/// </summary>
/// <param name="Start">The start of the range.</param>
/// <param name="End">The end of the range.</param>
[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record DateTimeOffsetRange(DateTimeOffset? Start, DateTimeOffset? End)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"DateTimeOffset Range From:{Start} To:{End}";
    }

    #region Create

    /// <summary>
    ///     Creates a new instance of <see cref="DateTimeOffsetRange" />.
    /// </summary>
    public static Result<DateTimeOffsetRange> Create(DateTimeOffset? start, DateTimeOffset? end)
    {
        return Result.Ok()
                     .Verify(start != null && end != null && start <= end, "Start should be less than or equals to end.")
                     .Map(() => new DateTimeOffsetRange(start, end));
    }

    #endregion

}
