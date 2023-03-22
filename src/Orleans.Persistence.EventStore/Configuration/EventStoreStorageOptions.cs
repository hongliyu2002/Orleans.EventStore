using EventStore.Client;
using Orleans.Storage;

namespace Orleans.Configuration;

/// <summary>
///     EventStore persistent storage options.
/// </summary>
public class EventStoreStorageOptions : IStorageProviderSerializerOptions
{
    /// <summary>
    ///     Whether or not to delete underlying state stream during a clear operation.
    /// </summary>
    public bool DeleteStateOnClear { get; set; }

    /// <summary>
    ///     Stage of silo lifecycle where storage should be initialized.  Storage must be initialized prior to use.
    /// </summary>
    public int InitStage { get; set; } = ServiceLifecycleStage.ApplicationServices;

    /// <summary>
    ///     The serializer used in serialize state to EventStore stream.
    /// </summary>
    public IGrainStorageSerializer GrainStorageSerializer { get; set; } = null!;

    /// <summary>
    ///     The EventStore client settings.
    /// </summary>
    [Redact]
    public EventStoreClientSettings ClientSettings { get; set; } = null!;

    /// <summary>
    ///     The user credentials that have permissions to append events.
    /// </summary>
    [Redact]
    public UserCredentials? Credentials { get; set; }
}
