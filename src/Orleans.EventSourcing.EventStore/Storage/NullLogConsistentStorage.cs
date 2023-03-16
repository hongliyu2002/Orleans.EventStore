using Orleans.Runtime;

namespace Orleans.EventSourcing.EventStore;

/// <inheritdoc />
public class NullLogConsistentStorage : ILogConsistentStorage
{
    /// <inheritdoc />
    public Task<int> AppendAsync<TLogEntry>(string grainTypeName, GrainId grainId, IEnumerable<TLogEntry> entries, int expectedVersion)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TLogEntry>> ReadAsync<TLogEntry>(string grainTypeName, GrainId grainId, int fromVersion, int length)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<int> GetLastVersionAsync(string grainTypeName, GrainId grainId)
    {
        throw new NotSupportedException();
    }
}
