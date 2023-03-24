using System.Text;
using EventStore.Client;
using FluentAssertions;
using Grpc.Core;
using NUnit.Framework;

namespace EventStore.UnitTests;

[TestFixture]
public class PersistentSubscriptionsClientTests
{
    public EventStorePersistentSubscriptionsClient? _client;
    public Guid _id;

    [OneTimeSetUp]
    public void Setup()
    {
        var clientSettings = EventStoreClientSettings.Create("esdb://123.60.184.85:2113?tls=false");
        _client = new EventStorePersistentSubscriptionsClient(clientSettings);
        _id = new Guid("888858d7-77e6-4ce1-846c-86d28ce34f78");
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _client!.DisposeAsync();
    }

    [Test]
    public async Task Should_Create_PersistentSubscription_Of_A_Stream()
    {
        try
        {
            await _client!.CreateToStreamAsync($"Snack-{_id}", "test-group", new PersistentSubscriptionSettings());
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.AlreadyExists)
        {
            await TestContext.Progress.WriteLineAsync(ex.Message);
        }
        catch (Exception ex)
        {
            await TestContext.Progress.WriteLineAsync(ex.GetType().Name);
            await TestContext.Progress.WriteLineAsync(ex.Message);
        }
    }

    [Test]
    public async Task Should_Create_PersistentSubscription_Of_All_Stream()
    {
        try
        {
            await _client!.CreateToAllAsync("test-group", StreamFilter.Prefix("Snack-"), new PersistentSubscriptionSettings());
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.AlreadyExists)
        {
            await TestContext.Progress.WriteLineAsync(ex.Message);
        }
        catch (Exception ex)
        {
            await TestContext.Progress.WriteLineAsync(ex.GetType().Name);
            await TestContext.Progress.WriteLineAsync(ex.Message);
        }
    }

    [Test]
    public async Task Should_Subscribe_To_PersistentSubscription_Of_A_Stream()
    {
        var subscription = await _client!.SubscribeToStreamAsync($"Snack-{_id}", "test-group", OnEventAppeared, OnSubscriptionDropped);
        subscription.Should().NotBeNull();
        await Task.Delay(5000);
    }

    [Test]
    public async Task Should_Subscribe_To_PersistentSubscription_Of_All_Stream()
    {
        var subscription = await _client!.SubscribeToAllAsync("test-group", OnEventAppeared, OnSubscriptionDropped);
        subscription.Should().NotBeNull();
        await Task.Delay(5000);
    }

    private static async Task OnEventAppeared(PersistentSubscription sub, ResolvedEvent evt, int? retryCount, CancellationToken ct)
    {
        try
        {
            var evtJson = Encoding.UTF8.GetString(evt.Event.Data.ToArray());
            await TestContext.Progress.WriteLineAsync($"{evt.Event.EventNumber} {evtJson}");
            await sub.Ack(evt);
        }
        catch (Exception ex)
        {
            await sub.Nack(PersistentSubscriptionNakEventAction.Park, ex.Message, evt);
        }
    }

    private static async void OnSubscriptionDropped(PersistentSubscription sub, SubscriptionDroppedReason reason, Exception? exception)
    {
        await TestContext.Progress.WriteLineAsync($"Subscription was dropped due to {reason}. {exception}");
    }
}
