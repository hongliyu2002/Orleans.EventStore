using Orleans.FluentResults;

namespace SiloX.Domain.Abstractions;

/// <summary>
///     Represents a range of <see cref="Int64" /> values.
/// </summary>
/// <param name="Start">The start of the range.</param>
/// <param name="End">The end of the range.</param>
[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record Int64Range(long? Start, long? End)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"Int64 Range From:{Start} To:{End}";
    }

    #region Create

    /// <summary>
    ///     Creates a new instance of <see cref="Int64Range" />.
    /// </summary>
    public static Result<Int64Range> Create(long? start, long? end)
    {
        return Result.Ok()
                     .Verify(start != null && end != null && start <= end, "Start should be less than or equals to end.")
                     .Map(() => new Int64Range(start, end));
    }

    #endregion

}
