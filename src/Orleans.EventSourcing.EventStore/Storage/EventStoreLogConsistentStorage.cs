using System.Text;
using EventStore.Client;
using JsonNet.PrivateSettersContractResolvers;
using Newtonsoft.Json;
using Orleans.Runtime;

namespace Orleans.EventSourcing.EventStore;

/// <inheritdoc />
public class EventStoreLogConsistentStorage : ILogConsistentStorage
{
    private static readonly JsonSerializerSettings _jsonDefaultSettings = new()
                                                                          {
                                                                              ContractResolver = new PrivateSetterContractResolver(),
                                                                              DefaultValueHandling = DefaultValueHandling.Include,
                                                                              TypeNameHandling = TypeNameHandling.None,
                                                                              NullValueHandling = NullValueHandling.Ignore,
                                                                              Formatting = Formatting.None,
                                                                              ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
                                                                          };

    private readonly EventStoreClient _eventStoreClient;

    /// <summary>
    ///     Initializes a new instance of EventStoreLogConsistentStorage class
    /// </summary>
    /// <param name="eventStoreClient"></param>
    public EventStoreLogConsistentStorage(EventStoreClient eventStoreClient)
    {
        ArgumentNullException.ThrowIfNull(eventStoreClient);
        _eventStoreClient = eventStoreClient;
    }

    /// <inheritdoc />
    public virtual async Task<int> AppendAsync<TLogEntry>(string grainTypeName, GrainId grainId, IEnumerable<TLogEntry> entries, int expectedVersion)
    {
        var logEntries = entries as TLogEntry[] ?? entries.ToArray();
        if (!logEntries.Any())
        {
            return expectedVersion;
        }
        var streamName = GetStreamName(grainTypeName, grainId);
        var eventData = logEntries.Select(SerializeEvent);
        var writeResult = await _eventStoreClient.AppendToStreamAsync(streamName, new StreamRevision((ulong)expectedVersion), eventData);
        return (int)writeResult.NextExpectedStreamRevision.ToUInt64();
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TLogEntry>> ReadAsync<TLogEntry>(string grainTypeName, GrainId grainId, int fromVersion, int length)
    {
        if (length <= 0)
        {
            return new List<TLogEntry>();
        }
        var streamName = GetStreamName(grainTypeName, grainId);
        var readResult = _eventStoreClient.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.FromInt64(fromVersion), length);
        var readState = await readResult.ReadState;
        if (readState == ReadState.StreamNotFound)
        {
            return new List<TLogEntry>();
        }
        return await readResult.Select(DeserializeEvent<TLogEntry>).OrderBy(x => x.Version).Select(x => x.LogEntry).ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task<int> GetLastVersionAsync(string grainTypeName, GrainId grainId)
    {
        var streamName = GetStreamName(grainTypeName, grainId);
        var readResult = _eventStoreClient.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1);
        var readState = await readResult.ReadState;
        if (readState == ReadState.StreamNotFound)
        {
            return -1;
        }
        var resolvedEvent = await readResult.FirstOrDefaultAsync();
        return (int)resolvedEvent.Event.EventNumber.ToUInt64();
    }

    /// <summary>
    /// </summary>
    /// <param name="grainTypeName"></param>
    /// <param name="grainId"></param>
    /// <returns></returns>
    protected virtual string GetStreamName(string grainTypeName, GrainId grainId)
    {
        return $"{grainTypeName}_{grainId.Key}";
    }

    /// <summary>
    /// </summary>
    /// <param name="entry"></param>
    /// <typeparam name="TLogEntry"></typeparam>
    /// <returns></returns>
    protected virtual EventData SerializeEvent<TLogEntry>(TLogEntry entry)
    {
        if (entry is null)
        {
            return new EventData(Uuid.NewUuid(), typeof(TLogEntry).Name, new ReadOnlyMemory<byte>());
        }
        var eventJson = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entry, _jsonDefaultSettings));
        return new EventData(Uuid.NewUuid(), entry.GetType().Name, new ReadOnlyMemory<byte>(eventJson));
    }

    /// <summary>
    /// </summary>
    /// <param name="evt"></param>
    /// <typeparam name="TLogEntry"></typeparam>
    /// <returns></returns>
    protected virtual (TLogEntry LogEntry, int Version) DeserializeEvent<TLogEntry>(ResolvedEvent evt)
    {
        var eventJson = Encoding.UTF8.GetString(evt.Event.Data.ToArray());
        var logEntry = JsonConvert.DeserializeObject<TLogEntry>(eventJson, _jsonDefaultSettings) ?? Activator.CreateInstance<TLogEntry>();
        return (logEntry, (int)evt.Event.EventNumber.ToUInt64());
    }
}
