using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Configuration.Overrides;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Factory for creating <see cref="EventStoreCheckpointer" /> instances.
/// </summary>
public class EventStoreCheckpointerFactory : IStreamQueueCheckpointerFactory
{
    private readonly string _providerName;
    private readonly EventStoreStreamCheckpointerOptions _options;
    private readonly Serializer _serializer;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ClusterOptions _clusterOptions;

    /// <summary>
    ///     Creates an instance of the <see cref="EventStoreCheckpointerFactory" /> using the provided IServiceProvider and providerName.
    /// </summary>
    /// <param name="serviceProvider">The IServiceProvider used for dependency injection.</param>
    /// <param name="providerName">The name used to retrieve options and services from the IServiceProvider.</param>
    /// <returns>An instance of the <see cref="EventStoreCheckpointerFactory" />.</returns>
    public static EventStoreCheckpointerFactory CreateFactory(IServiceProvider serviceProvider, string providerName)
    {
        var options = serviceProvider.GetOptionsByName<EventStoreStreamCheckpointerOptions>(providerName);
        var clusterOptions = serviceProvider.GetProviderClusterOptions(providerName);
        return ActivatorUtilities.CreateInstance<EventStoreCheckpointerFactory>(serviceProvider, providerName, options, clusterOptions);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreCheckpointerFactory" /> class with the specified parameters.
    /// </summary>
    /// <param name="providerName">The name of the stream provider.</param>
    /// <param name="options">The options for the stream checkpointer.</param>
    /// <param name="clusterOptions">The cluster options.</param>
    /// <param name="serializer">The serializer used for state manager.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    private EventStoreCheckpointerFactory(string providerName, EventStoreStreamCheckpointerOptions options, IOptions<ClusterOptions> clusterOptions, Serializer serializer, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerName, nameof(providerName));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(clusterOptions, nameof(clusterOptions));
        ArgumentNullException.ThrowIfNull(serializer, nameof(serializer));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        _providerName = providerName;
        _options = options;
        _clusterOptions = clusterOptions.Value;
        _serializer = serializer;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    ///     Creates a stream checkpointer for the specified queue.
    /// </summary>
    /// <param name="queue">The queue name.</param>
    /// <returns>The stream checkpointer.</returns>
    public Task<IStreamQueueCheckpointer<string>> Create(string queue)
    {
        return Task.FromResult<IStreamQueueCheckpointer<string>>(EventStoreCheckpointer.Create(_clusterOptions.ServiceId, _providerName, queue, _options, _serializer, _loggerFactory));
    }

}
