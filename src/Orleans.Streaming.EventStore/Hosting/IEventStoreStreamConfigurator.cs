namespace Orleans.Hosting;

/// <summary>
///     Configures an EventStore stream for a named service.
/// </summary>
public interface IEventStoreStreamConfigurator : INamedServiceConfigurator
{
}

/// <summary>
///     Configures an EventStore stream for a silo and provides recovery options.
/// </summary>
public interface ISiloEventStoreStreamConfigurator : IEventStoreStreamConfigurator, ISiloRecoverableStreamConfigurator
{
}

/// <summary>
///     Configures an EventStore stream for a cluster client and provides persistent stream options.
/// </summary>
public interface IClusterClientEventStoreStreamConfigurator : IEventStoreStreamConfigurator, IClusterClientPersistentStreamConfigurator
{
}
