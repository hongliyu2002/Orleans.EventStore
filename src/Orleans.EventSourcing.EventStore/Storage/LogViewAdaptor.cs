using System.Collections.Immutable;
using System.Diagnostics;
using Force.DeepCloner;
using Microsoft.Extensions.Logging;
using Orleans.EventSourcing.Common;
using Orleans.Storage;

namespace Orleans.EventSourcing.EventStore;

/// <summary>
///     A log view adaptor that wraps around a traditional storage adaptor, and uses batching and e-tags
///     to append entries.
///     <para>
///         The log itself is transient, i.e. not actually saved to storage - only the latest view and some
///         metadata (the log position, and write flags) are stored.
///     </para>
/// </summary>
/// <typeparam name="TLogView">Type of log view</typeparam>
/// <typeparam name="TLogEntry">Type of log entry</typeparam>
internal class LogViewAdaptor<TLogView, TLogEntry> : PrimaryBasedLogViewAdaptor<TLogView, TLogEntry, SubmissionEntry<TLogEntry>>
    where TLogView : class, new()
    where TLogEntry : class
{
    private readonly IGrainStorage _grainStorage;
    private readonly string _grainTypeName;
    private readonly ILogConsistentStorage _logStorage;

    private SnapshotStateWithMetaDataAndETag<TLogView> _globalSnapshot;
    private TLogView _confirmedView;
    private int _confirmedVersion;
    private int _globalVersion;

    /// <summary>
    ///     Initialize a StorageProviderLogViewAdaptor class
    /// </summary>
    public LogViewAdaptor(ILogViewAdaptorHost<TLogView, TLogEntry> host, TLogView initialState, IGrainStorage grainStorage, string grainTypeName, ILogConsistencyProtocolServices services, ILogConsistentStorage logStorage)
        : base(host, initialState, services)
    {
        ArgumentNullException.ThrowIfNull(grainStorage);
        ArgumentException.ThrowIfNullOrEmpty(grainTypeName);
        ArgumentNullException.ThrowIfNull(logStorage);
        _grainStorage = grainStorage;
        _grainTypeName = grainTypeName;
        _logStorage = logStorage;
    }

    /// <inheritdoc />
    protected override void InitializeConfirmedView(TLogView initialState)
    {
        _globalSnapshot = new SnapshotStateWithMetaDataAndETag<TLogView>(initialState);
        _confirmedView = initialState;
        _confirmedVersion = 0;
        _globalVersion = 0;
    }

    /// <inheritdoc />
    protected override TLogView LastConfirmedView()
    {
        return _confirmedView;
    }

    /// <inheritdoc />
    protected override int GetConfirmedVersion()
    {
        return _confirmedVersion;
    }

    /// <inheritdoc />
    public override Task<IReadOnlyList<TLogEntry>> RetrieveLogSegment(int fromVersion, int length)
    {
        return _logStorage.ReadAsync<TLogEntry>(_grainTypeName, Services.GrainId, fromVersion, length);
    }

    private void UpdateConfirmedView(IReadOnlyList<TLogEntry> logEntries)
    {
        foreach (var logEntry in logEntries)
        {
            try
            {
                Host.UpdateView(_confirmedView, logEntry);
            }
            catch (Exception ex)
            {
                Services.CaughtUserCodeException("UpdateView", nameof(UpdateConfirmedView), ex);
            }
        }
        _confirmedVersion += logEntries.Count;
    }

    /// <inheritdoc />
    protected override async Task ReadAsync()
    {
        enter_operation("ReadAsync");
        while (true)
        {
            try
            {
                var snapshot = new SnapshotStateWithMetaDataAndETag<TLogView>();
                await _grainStorage.ReadStateAsync(_grainTypeName, Services.GrainId, snapshot);
                _globalSnapshot = snapshot;
                Services.Log(LogLevel.Debug, "read success {0}", _globalSnapshot);
                if (_confirmedVersion < _globalSnapshot.State.SnapshotVersion)
                {
                    _confirmedVersion = _globalSnapshot.State.SnapshotVersion;
                    _confirmedView = _globalSnapshot.State.Snapshot.DeepClone();
                }
                try
                {
                    _globalVersion = await _logStorage.GetLastVersionAsync(_grainTypeName, Services.GrainId);
                    if (_confirmedVersion < _globalVersion)
                    {
                        var logEntries = await _logStorage.ReadAsync<TLogEntry>(_grainTypeName, Services.GrainId, _confirmedVersion, _globalVersion - _confirmedVersion);
                        Services.Log(LogLevel.Debug, "read success {0}", logEntries);
                        UpdateConfirmedView(logEntries);
                    }
                    LastPrimaryIssue.Resolve(Host, Services);
                    break; // successful
                }
                catch (Exception ex)
                {
                    LastPrimaryIssue.Record(new ReadFromLogStorageFailed { Exception = ex }, Host, Services);
                }
            }
            catch (Exception ex)
            {
                LastPrimaryIssue.Record(new ReadFromSnapshotStorageFailed { Exception = ex }, Host, Services);
            }
            Services.Log(LogLevel.Debug, "read failed {0}", LastPrimaryIssue);
            await LastPrimaryIssue.DelayBeforeRetry();
        }
        exit_operation("ReadAsync");
    }

    /// <inheritdoc />
    protected override async Task<int> WriteAsync()
    {
        enter_operation("WriteAsync");
        var updates = GetCurrentBatchOfUpdates();
        var batchSuccessfullyWritten = false;
        var logsSuccessfullyAppended = false;
        var writebit = _globalSnapshot.State.FlipBit(Services.MyClusterId);
        try
        {
            var logEntries = updates.Select(x => x.Entry).ToImmutableList();
            _globalVersion = await _logStorage.AppendAsync(_grainTypeName, Services.GrainId, logEntries, _globalVersion);
            logsSuccessfullyAppended = true;
            Services.Log(LogLevel.Debug, "write success {0}", logEntries);
            UpdateConfirmedView(logEntries);
        }
        catch (Exception ex)
        {
            LastPrimaryIssue.Record(new UpdateLogStorageFailed { Exception = ex }, Host, Services);
        }
        if (logsSuccessfullyAppended)
        {
            try
            {
                _globalSnapshot.State.Snapshot = _confirmedView.DeepClone();
                _globalSnapshot.State.SnapshotVersion = _confirmedVersion;
                await _grainStorage.WriteStateAsync(_grainTypeName, Services.GrainId, _globalSnapshot);
                batchSuccessfullyWritten = true;
                Services.Log(LogLevel.Debug, "write ({0} updates) success {1}", updates.Length, _globalSnapshot);
                LastPrimaryIssue.Resolve(Host, Services);
            }
            catch (Exception ex)
            {
                LastPrimaryIssue.Record(new UpdateSnapshotStorageFailed { Exception = ex }, Host, Services);
            }
        }
        if (!batchSuccessfullyWritten)
        {
            Services.Log(LogLevel.Debug, "write apparently failed {0}", LastPrimaryIssue);
            while (true) // be stubborn until we can read what is there
            {
                await LastPrimaryIssue.DelayBeforeRetry();
                try
                {
                    var snapshot = new SnapshotStateWithMetaDataAndETag<TLogView>();
                    await _grainStorage.ReadStateAsync(_grainTypeName, Services.GrainId, snapshot);
                    _globalSnapshot = snapshot;
                    Services.Log(LogLevel.Debug, "read success {0}", _globalSnapshot);
                    if (_confirmedVersion < _globalSnapshot.State.SnapshotVersion)
                    {
                        _confirmedVersion = _globalSnapshot.State.SnapshotVersion;
                        _confirmedView = _globalSnapshot.State.Snapshot.DeepClone();
                    }
                    try
                    {
                        // _globalVersion = await _logStorage.GetLastVersionAsync(_grainTypeName, Services.GrainId);
                        if (_confirmedVersion < _globalVersion)
                        {
                            var logEntries = await _logStorage.ReadAsync<TLogEntry>(_grainTypeName, Services.GrainId, _confirmedVersion, _globalVersion - _confirmedVersion);
                            Services.Log(LogLevel.Debug, "read success {0}", logEntries);
                            UpdateConfirmedView(logEntries);
                        }
                        LastPrimaryIssue.Resolve(Host, Services);
                        break; // successful
                    }
                    catch (Exception ex)
                    {
                        LastPrimaryIssue.Record(new ReadFromLogStorageFailed { Exception = ex }, Host, Services);
                    }
                }
                catch (Exception ex)
                {
                    LastPrimaryIssue.Record(new ReadFromSnapshotStorageFailed { Exception = ex }, Host, Services);
                }
                Services.Log(LogLevel.Debug, "read failed {0}", LastPrimaryIssue);
            }
            // check if last apparently failed write was in fact successful
            if (writebit == _globalSnapshot.State.GetBit(Services.MyClusterId))
            {
                Services.Log(LogLevel.Debug, "last write ({0} updates) was actually a success {1}", updates.Length, _globalSnapshot);
                batchSuccessfullyWritten = true;
            }
        }
        exit_operation("WriteAsync");
        return batchSuccessfullyWritten ? updates.Length : 0;
    }

    /// <inheritdoc />
    protected override SubmissionEntry<TLogEntry> MakeSubmissionEntry(TLogEntry entry)
    {
        return new SubmissionEntry<TLogEntry> { Entry = entry };
    }

    #region Operation Failed Classes

    /// <summary>
    ///     Describes a connection issue that occurred when reading from the primary storage.
    /// </summary>
    [Serializable]
    [GenerateSerializer]
    public sealed class ReadFromSnapshotStorageFailed : PrimaryOperationFailed
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return $"read state from snapshot storage failed: caught {Exception.GetType().Name}: {Exception.Message}";
        }
    }

    /// <summary>
    ///     Describes a connection issue that occurred when updating the primary storage.
    /// </summary>
    [Serializable]
    [GenerateSerializer]
    public sealed class UpdateSnapshotStorageFailed : PrimaryOperationFailed
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return $"write state to snapshot storage failed: caught {Exception.GetType().Name}: {Exception.Message}";
        }
    }

    /// <summary>
    ///     Describes a connection issue that occurred when reading from the primary storage.
    /// </summary>
    [Serializable]
    [GenerateSerializer]
    public sealed class ReadFromLogStorageFailed : PrimaryOperationFailed
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return $"read logs from storage failed: caught {Exception.GetType().Name}: {Exception.Message}";
        }
    }

    /// <summary>
    ///     Describes a connection issue that occurred when updating the primary storage.
    /// </summary>
    [Serializable]
    [GenerateSerializer]
    public sealed class UpdateLogStorageFailed : PrimaryOperationFailed
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return $"write logs to storage failed: caught {Exception.GetType().Name}: {Exception.Message}";
        }
    }

    #endregion

    #region Debug

#if DEBUG
    private bool operation_in_progress;
#endif

    [Conditional("DEBUG")]
    private void enter_operation(string name)
    {
    #if DEBUG
        Services.Log(LogLevel.Trace, "/-- enter {0}", name);
        Debug.Assert(!operation_in_progress);
        operation_in_progress = true;
    #endif
    }

    [Conditional("DEBUG")]
    private void exit_operation(string name)
    {
    #if DEBUG
        Services.Log(LogLevel.Trace, "\\-- exit {0}", name);
        Debug.Assert(operation_in_progress);
        operation_in_progress = false;
    #endif
    }

    #endregion

}
