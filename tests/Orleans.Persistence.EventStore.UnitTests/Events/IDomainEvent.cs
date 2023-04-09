namespace SiloX.Domain.Abstractions;

/// <summary>
///     An interface represents a domain event.
/// </summary>
public interface IDomainEvent : ITraceable
{
    /// <summary>
    ///     The version of the domain event.
    /// </summary>
    int Version { get; init; }
}
