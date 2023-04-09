using Orleans.FluentResults;

namespace SiloX.Domain.Abstractions;

/// <summary>
///     Represents a range of <see cref="Double" /> values.
/// </summary>
/// <param name="Start">The start of the range.</param>
/// <param name="End">The end of the range.</param>
[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record DoubleRange(double? Start, double? End)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"Double Range From:{Start} To:{End}";
    }

    #region Create

    /// <summary>
    ///     Creates a new instance of <see cref="DoubleRange" />.
    /// </summary>
    public static Result<DoubleRange> Create(double? start, double? end)
    {
        return Result.Ok()
                     .Verify(start != null && end != null && start <= end, "Start should be less than or equals to end.")
                     .Map(() => new DoubleRange(start, end));
    }

    #endregion

}
