using Orleans.FluentResults;

namespace SiloX.Domain.Abstractions;

/// <summary>
///     Represents a range of <see cref="Decimal" /> values.
/// </summary>
/// <param name="Start">The start of the range.</param>
/// <param name="End">The end of the range.</param>
[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record DecimalRange(decimal? Start, decimal? End)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"Decimal Range From:{Start} To:{End}";
    }

    #region Create

    /// <summary>
    ///     Creates a new instance of <see cref="DecimalRange" />.
    /// </summary>
    public static Result<DecimalRange> Create(decimal? start, decimal? end)
    {
        return Result.Ok()
                     .Verify(start != null && end != null && start <= end, "Start should be less than or equals to end.")
                     .Map(() => new DecimalRange(start, end));
    }

    #endregion

}
