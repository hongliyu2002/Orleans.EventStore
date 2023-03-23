using FluentAssertions;
using NUnit.Framework;
using Orleans.Streaming.EventStore.UnitTests.Grains;
using Orleans.Streaming.EventStore.UnitTests.Hosts;
using Orleans.TestingHost;

namespace Orleans.Streaming.EventStore.UnitTests;

[TestFixture]
public class ChannelGrainTests
{
    private TestCluster _cluster = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var clusterBuilder = new TestClusterBuilder().AddClientBuilderConfigurator<ClientBuilderConfigurator>().AddSiloBuilderConfigurator<SiloConfigurator>();
        clusterBuilder.Options.ServiceId = "TestService";
        clusterBuilder.Options.InitialSilosCount = 1;
        _cluster = clusterBuilder.Build();
        await _cluster.DeployAsync();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _cluster.DisposeAsync();
    }

    [Test]
    public async Task Join_And_Send_Message_Test()
    {
        // Arrange
        var channel = _cluster.Client.GetGrain<IChannelGrain>(Guid.NewGuid());
        var nickname = "Boss";
        var sentMessage = new ChatMessage(nickname, "Fuck, everyone!", DateTimeOffset.UtcNow);

        // Act
        var streamId = await channel.Join(nickname);
        await channel.SendMessage(sentMessage);
        var history = await channel.ReadHistory(10);

        // Assert
        history.Should().Contain(m => m.Author == sentMessage.Author && m.Text == sentMessage.Text);
    }

    [TestCase("Boss", "Hey, everyone!")]
    [TestCase("UserA", "Hello!")]
    [TestCase("UserB", "What's up?")]
    public async Task SendMessage_Test(string author, string text)
    {
        // Arrange
        var channel = _cluster.Client.GetGrain<IChannelGrain>(Guid.NewGuid());
        await channel.Join(author);
        var sentMessage = new ChatMessage(author, text, DateTimeOffset.UtcNow);

        // Act
        await channel.SendMessage(sentMessage);
        var history = await channel.ReadHistory(10);

        // Assert
        history.Should().ContainSingle(m => m.Author == author && m.Text == text);
    }

    [Test]
    public async Task Leave_Test()
    {
        // Arrange
        var channel = _cluster.Client.GetGrain<IChannelGrain>(Guid.NewGuid());
        var nickname = "TestUser";
        await channel.Join(nickname);

        // Act
        await channel.Leave(nickname);
        var members = await channel.GetMembers();

        // Assert
        members.Should().NotContain(nickname);
    }

    [Test]
    public async Task Multiple_Clients_Test()
    {
        // Arrange
        var channel = _cluster.Client.GetGrain<IChannelGrain>(Guid.NewGuid());
        var nicknames = new[]
                        {
                            "UserA",
                            "UserB",
                            "UserC"
                        };

        // Act
        foreach (var nickname in nicknames)
        {
            await channel.Join(nickname);
        }
        var members = await channel.GetMembers();

        // Assert
        members.Should().BeEquivalentTo(nicknames);

        // Act
        foreach (var nickname in nicknames)
        {
            await channel.Leave(nickname);
        }
        members = await channel.GetMembers();

        // Assert
        members.Should().NotContain(nicknames);
    }

    [Test]
    public async Task Chat_History_Test()
    {
        // Arrange
        var channel = _cluster.Client.GetGrain<IChannelGrain>(Guid.NewGuid());
        var nickname = "TestUser";
        var sentMessages = new List<ChatMessage>
                           {
                               new(nickname, "Hello, everyone!", DateTimeOffset.UtcNow),
                               new(nickname, "How are you?", DateTimeOffset.UtcNow.AddSeconds(1)),
                               new(nickname, "Bye!", DateTimeOffset.UtcNow.AddSeconds(2))
                           };

        // Act
        await channel.Join(nickname);
        foreach (var message in sentMessages)
        {
            await channel.SendMessage(message);
        }
        var history = await channel.ReadHistory(10);

        // Assert
        history.Should().BeEquivalentTo(sentMessages);
    }

    [Test]
    public async Task Test_Send_Message_And_Subscribe()
    {
        // Arrange
        var nickname = "TestUser";
        var messageText = "Hello, world!";
        var grainFactory = _cluster.GrainFactory;
        var channelGrain = grainFactory.GetGrain<IChannelGrain>(Guid.NewGuid());
        var subscriberGrain = grainFactory.GetGrain<ISubscriberGrain>(Guid.NewGuid());

        // Act
        var streamId = await channelGrain.Join(nickname);
        await subscriberGrain.Subscribe(streamId);
        for (var i = 0; i < 5; i++)
        {
            await channelGrain.SendMessage(new ChatMessage(nickname, $"{messageText} {i}", DateTimeOffset.UtcNow));
            await Task.Delay(500);
        }
        await channelGrain.Leave(nickname);
        await subscriberGrain.Unsubscribe();

        // Assert
        var history = await channelGrain.ReadHistory(10);
        history.Length.Should().BeGreaterOrEqualTo(5);
        history.Should().Contain(x => x.Author == nickname && x.Text.StartsWith($"{messageText}"));
    }
}
