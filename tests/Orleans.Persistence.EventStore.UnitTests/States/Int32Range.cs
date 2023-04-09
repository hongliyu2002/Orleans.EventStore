using Orleans.FluentResults;

namespace SiloX.Domain.Abstractions;

/// <summary>
///     Represents a range of <see cref="Int32" /> values.
/// </summary>
/// <param name="Start">The start of the range.</param>
/// <param name="End">The end of the range.</param>
[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record Int32Range(int? Start, int? End)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"Int32 Range From:{Start} To:{End}";
    }

    #region Create

    /// <summary>
    ///     Creates a new instance of <see cref="Int32Range" />.
    /// </summary>
    public static Result<Int32Range> Create(int? start, int? end)
    {
        return Result.Ok()
                     .Verify(start != null && end != null && start <= end, "Start should be less than or equals to end.")
                     .Map(() => new Int32Range(start, end));
    }

    #endregion

}
