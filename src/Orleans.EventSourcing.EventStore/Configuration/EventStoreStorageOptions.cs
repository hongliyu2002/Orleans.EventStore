using EventStore.Client;
using Orleans.Storage;

namespace Orleans.EventSourcing.Configuration;

/// <summary>
///     EventStore log consistent storage options.
/// </summary>
public class EventStoreStorageOptions : IStorageProviderSerializerOptions
{
    /// <summary>
    ///     Stage of silo lifecycle where storage should be initialized.  Storage must be initialized prior to use.
    /// </summary>
    public int InitStage { get; set; } = ServiceLifecycleStage.ApplicationServices;

    /// <inheritdoc />
    public IGrainStorageSerializer GrainStorageSerializer { get; set; } = null!;

    /// <summary>
    ///     The EventStore client settings.
    /// </summary>
    [Redact]
    public EventStoreClientSettings ClientSettings { get; set; } = null!;
}
