using Orleans.FluentResults;

namespace SiloX.Domain.Abstractions;

/// <summary>
///     Represents a range of <see cref="Single" /> values.
/// </summary>
/// <param name="Start">The start of the range.</param>
/// <param name="End">The end of the range.</param>
[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SingleRange(float? Start, float? End)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"Single Range From:{Start} To:{End}";
    }

    #region Create

    /// <summary>
    ///     Creates a new instance of <see cref="SingleRange" />.
    /// </summary>
    public static Result<SingleRange> Create(float? start, float? end)
    {
        return Result.Ok()
                     .Verify(start != null && end != null && start <= end, "Start should be less than or equals to end.")
                     .Map(() => new SingleRange(start, end));
    }

    #endregion

}
