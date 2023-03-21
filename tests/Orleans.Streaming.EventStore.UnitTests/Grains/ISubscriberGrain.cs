using Orleans.Runtime;

namespace Orleans.Streaming.EventStore.UnitTests.Grains;

public interface ISubscriberGrain : IGrainWithGuidKey
{
    Task Subscribe(StreamId streamId);

    Task Unsubscribe();
}
