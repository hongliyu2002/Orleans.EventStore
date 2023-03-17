using System.Text;
using System.Text.Json;
using EventStore.Client;
using EventStore.UnitTests.Events;
using FluentAssertions;
using NUnit.Framework;

namespace EventStore.UnitTests;

[TestFixture]
public class EventStoreClientTests
{
    public EventStoreClient? _client;
    public Guid _id;

    [OneTimeSetUp]
    public void Setup()
    {
        var clientSettings = EventStoreClientSettings.Create("esdb://123.60.184.85:2113?tls=false");
        _client = new EventStoreClient(clientSettings);
        _id = new Guid("888858d7-77e6-4ce1-846c-86d28ce34f78");
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _client!.DisposeAsync();
    }

    [Test]
    public async Task Should_Append_Data_To_Stream()
    {
        var evt = new SnackInitializedEvent(_id, "Cafe", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Append_Data_To_Stream", 0);
        var evtData = new EventData(Uuid.NewUuid(), nameof(SnackInitializedEvent), JsonSerializer.SerializeToUtf8Bytes(evt));
        var writeResult = await _client!.AppendToStreamAsync($"Snack-{evt.Id}", StreamState.Any, new[] { evtData });
        await TestContext.Progress.WriteLineAsync(writeResult.NextExpectedStreamRevision.ToString());
    }

    [Test]
    public async Task Should_Not_Append_Data_To_Stream_When_In_Concurrency()
    {
        var readResult = _client!.ReadStreamAsync(Direction.Backwards, $"Snack-{_id}", StreamPosition.End, 1);
        var readRevision = (await readResult.FirstOrDefaultAsync()).Event.EventNumber.ToUInt64();
        readRevision.Should().NotBe(0);
        var evt1 = new SnackNameChangedEvent(_id, "Coke", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Not_Append_Data_To_Stream_When_In_Concurrency", (int)readRevision);
        var evt1Data = new EventData(Uuid.NewUuid(), nameof(SnackNameChangedEvent), JsonSerializer.SerializeToUtf8Bytes(evt1));
        var evt1Result = await _client!.AppendToStreamAsync($"Snack-{evt1.Id}", new StreamRevision(readRevision), new[] { evt1Data });
        await TestContext.Progress.WriteLineAsync(evt1Result.NextExpectedStreamRevision.ToString());
        var evt2 = new SnackRemovedEvent(_id, Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Not_Append_Data_To_Stream_When_In_Concurrency", (int)readRevision);
        var evt2Data = new EventData(Uuid.NewUuid(), nameof(SnackRemovedEvent), JsonSerializer.SerializeToUtf8Bytes(evt2));
        var evt2Action = async () =>
                         {
                             var evt2Result = await _client!.AppendToStreamAsync($"Snack-{evt2.Id}", new StreamRevision(readRevision), new[] { evt2Data });
                             await TestContext.Progress.WriteLineAsync(evt2Result.NextExpectedStreamRevision.ToString());
                         };
        await evt2Action.Should().ThrowAsync<WrongExpectedVersionException>();
    }

    [Test]
    public async Task Should_Read_Data_From_Stream()
    {
        var readResult = _client!.ReadStreamAsync(Direction.Forwards, $"Snack-{_id}", StreamPosition.Start);
        (await readResult.ReadState).Should().Be(ReadState.Ok);
        await foreach (var evt in readResult)
        {
            var evtJson = Encoding.UTF8.GetString(evt.Event.Data.ToArray());
            // var evtData = JsonSerializer.Deserialize<SnackInitializedEvent>(evtJson);
            await TestContext.Progress.WriteLineAsync(evtJson);
        }
    }

    [Test]
    public async Task Should_Subscribe_To_Stream()
    {
        static async Task OnEventAppeared(StreamSubscription sub, ResolvedEvent evt, CancellationToken ct)
        {
            var evtJson = Encoding.UTF8.GetString(evt.Event.Data.ToArray());
            await TestContext.Progress.WriteLineAsync($"{evt.Event.EventNumber} {evtJson}");
        }
        static async void OnSubscriptionDropped(StreamSubscription subscription, SubscriptionDroppedReason reason, Exception exception)
        {
            await TestContext.Progress.WriteLineAsync($"Subscription was dropped due to {reason}. {exception}");
        }
        await _client!.SubscribeToStreamAsync($"Snack-{_id}", FromStream.Start, OnEventAppeared, true, OnSubscriptionDropped!);
        await Task.Delay(1000);
    }

    [Test]
    public async Task Should_Delete_Stream()
    {
        var id = Guid.NewGuid();
        var evt = new SnackInitializedEvent(id, "Cafe", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Delete_Stream", -1);
        var evtData = new EventData(Uuid.NewUuid(), nameof(SnackInitializedEvent), JsonSerializer.SerializeToUtf8Bytes(evt));
        var evtResult = await _client!.AppendToStreamAsync($"Snack-{id}", StreamRevision.None, new[] { evtData });
        var version = evtResult.NextExpectedStreamRevision.ToUInt64();
        version.Should().Be(0);

        await TestContext.Progress.WriteLineAsync(version.ToString());
        var evt1 = new SnackRemovedEvent(id, Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Delete_Stream", (int)version);
        var evt1Data = new EventData(Uuid.NewUuid(), nameof(SnackNameChangedEvent), JsonSerializer.SerializeToUtf8Bytes(evt1));
        var evt1Result = await _client!.AppendToStreamAsync($"Snack-{id}", new StreamRevision(version), new[] { evt1Data });
        var version1 = evt1Result.NextExpectedStreamRevision.ToUInt64();
        version1.Should().Be(1);
        
        await TestContext.Progress.WriteLineAsync(version1.ToString());
        var deleteResult = await _client.DeleteAsync($"Snack-{id}", new StreamRevision(version1));
        await TestContext.Progress.WriteLineAsync(deleteResult.LogPosition.ToString());
        
        var evt2 = new SnackNameChangedEvent(id, "Coke", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Delete_Stream", (int)version1);
        var evt2Data = new EventData(Uuid.NewUuid(), nameof(SnackNameChangedEvent), JsonSerializer.SerializeToUtf8Bytes(evt2));
        var evt2Result = await _client!.AppendToStreamAsync($"Snack-{id}", new StreamRevision(version1), new[] { evt2Data });
        var version2 = evt2Result.NextExpectedStreamRevision.ToUInt64();
        version2.Should().Be(2);
        await TestContext.Progress.WriteLineAsync(version2.ToString());
    }
}
