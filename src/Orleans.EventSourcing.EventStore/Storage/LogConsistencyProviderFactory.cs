using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;

namespace Orleans.EventSourcing.EventStoreStorage;

/// <summary>
///     Factory used to create instances of log consistent provider.
/// </summary>
public static class LogConsistencyProviderFactory
{
    /// <summary>
    ///     Creates a EventStore log consistent storage instance.
    /// </summary>
    public static LogConsistencyProvider Create(IServiceProvider serviceProvider, string name)
    {
        var logConsistentStorage = serviceProvider.GetRequiredServiceByName<ILogConsistentStorage>(name);
        var logConsistencyProvider = ActivatorUtilities.CreateInstance<LogConsistencyProvider>(serviceProvider, logConsistentStorage);
        return logConsistencyProvider;
    }
}
