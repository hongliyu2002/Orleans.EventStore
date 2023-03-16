using Orleans.EventSourcing.Common;

namespace Orleans.EventSourcing.EventStore;

/// <summary>
///     A class that extends snapshot state with versioning metadata, so that a grain with log-view consistency
///     can use a standard storage provider via <see cref="LogViewAdaptor{TView,TEntry}" />
/// </summary>
/// <typeparam name="TView">The type used for log view</typeparam>
[Serializable]
[GenerateSerializer]
public sealed class SnapshotWithMetaDataAndETag<TView> : IGrainState<SnapshotWithMetaData<TView>>
    where TView : class, new()
{
    /// <summary>
    ///     Initializes a new instance of SnapshotWithMetaDataAndETag class
    /// </summary>
    public SnapshotWithMetaDataAndETag()
    {
        State = new SnapshotWithMetaData<TView>();
    }

    /// <summary>
    ///     Initialize a new instance of SnapshotWithMetaDataAndETag class with an initialView
    /// </summary>
    /// <param name="initialView"></param>
    public SnapshotWithMetaDataAndETag(TView initialView)
    {
        State = new SnapshotWithMetaData<TView>(initialView);
    }

    /// <inheritdoc />
    [Id(0)]
    public string ETag { get; set; } = null!;

    /// <inheritdoc />
    [Id(1)]
    public bool RecordExists { get; set; }

    /// <inheritdoc />
    [Id(2)]
    public SnapshotWithMetaData<TView> State { get; set; }

    /// <summary>
    ///     Convert current SnapshotWithMetaDataAndETag object information to a string
    /// </summary>
    public override string ToString()
    {
        return $"v{State.SnapshotVersion} Flags={State.WriteVector} ETag={ETag} Data={State.Snapshot}";
    }
}

/// <summary>
///     A class that extends grain state with versioning metadata, so that a log-consistent grain
///     can use a standard storage provider via <see cref="LogViewAdaptor{TView,TEntry}" />
/// </summary>
/// <typeparam name="TView"></typeparam>
[Serializable]
[GenerateSerializer]
public sealed class SnapshotWithMetaData<TView>
    where TView : class, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotWithMetaData{TView}" /> class.
    /// </summary>
    public SnapshotWithMetaData()
    {
        Snapshot = new TView();
        SnapshotVersion = 0;
        WriteVector = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotWithMetaData{TView}" /> class.
    /// </summary>
    public SnapshotWithMetaData(TView initialState)
    {
        Snapshot = initialState;
        SnapshotVersion = 0;
        WriteVector = string.Empty;
    }

    /// <summary>
    ///     The stored snapshot view of the log
    /// </summary>
    [Id(0)]
    public TView Snapshot { get; set; }

    /// <summary>
    ///     The snapshot length of the log
    /// </summary>
    [Id(1)]
    public int SnapshotVersion { get; set; }

    /// <summary>
    ///     Metadata that is used to avoid duplicate appends.
    ///     Logically, this is a (string->bit) map, the keys being replica ids
    ///     But this map is represented compactly as a simple string to reduce serialization/deserialization overhead
    ///     Bits are read by <see cref="GetBit" /> and flipped by  <see cref="FlipBit" />.
    ///     Bits are toggled when writing, so that the retry logic can avoid appending an entry twice
    ///     when retrying a failed append.
    /// </summary>
    [Id(2)]
    public string WriteVector { get; set; }

    /// <summary>
    ///     Gets one of the bits in <see cref="WriteVector" />
    /// </summary>
    /// <param name="replica">The replica for which we want to look up the bit</param>
    /// <returns></returns>
    public bool GetBit(string replica)
    {
        return StringEncodedWriteVector.GetBit(WriteVector, replica);
    }

    /// <summary>
    ///     toggle one of the bits in <see cref="WriteVector" /> and return the new value.
    /// </summary>
    /// <param name="replica">The replica for which we want to flip the bit</param>
    /// <returns>the state of the bit after flipping it</returns>
    public bool FlipBit(string replica)
    {
        var str = WriteVector;
        var rval = StringEncodedWriteVector.FlipBit(ref str, replica);
        WriteVector = str;
        return rval;
    }
}
