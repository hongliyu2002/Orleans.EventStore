namespace Orleans.Hosting;

/// <summary>
/// </summary>
public interface IEventStoreQueueStreamConfigurator : INamedServiceConfigurator
{
}

/// <summary>
/// </summary>
public interface ISiloEventStoreQueueStreamConfigurator : IEventStoreQueueStreamConfigurator, ISiloPersistentStreamConfigurator
{
}

/// <summary>
/// </summary>
public interface IClusterClientEventStoreQueueStreamConfigurator : IEventStoreQueueStreamConfigurator, IClusterClientPersistentStreamConfigurator
{
}
