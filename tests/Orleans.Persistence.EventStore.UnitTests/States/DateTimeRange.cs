using Orleans.FluentResults;

namespace SiloX.Domain.Abstractions;

/// <summary>
///     Represents a range of <see cref="DateTime" /> values.
/// </summary>
/// <param name="Start">The start of the range.</param>
/// <param name="End">The end of the range.</param>
[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record DateTimeRange(DateTime? Start, DateTime? End)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"DateTime Range From:{Start} To:{End}";
    }

    #region Create

    /// <summary>
    ///     Creates a new instance of <see cref="DateTimeRange" />.
    /// </summary>
    public static Result<DateTimeRange> Create(DateTime? start, DateTime? end)
    {
        return Result.Ok()
                     .Verify(start != null && end != null && start <= end, "Start should be less than or equals to end.")
                     .Map(() => new DateTimeRange(start, end));
    }

    #endregion

}
