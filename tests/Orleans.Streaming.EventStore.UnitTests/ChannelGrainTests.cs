using FluentAssertions;
using NUnit.Framework;
using Orleans.Streaming.EventStore.UnitTests.Grains;
using Orleans.Streaming.EventStore.UnitTests.Hosts;
using Orleans.TestingHost;

namespace Orleans.Streaming.EventStore.UnitTests;

[TestFixture]
public class ChannelGrainTests
{
    public TestCluster Cluster { get; set; } = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var clusterBuilder = new TestClusterBuilder().AddClientBuilderConfigurator<ClientBuilderConfigurator>().AddSiloBuilderConfigurator<SiloConfigurator>();
        clusterBuilder.Options.ServiceId = "TestService";
        Cluster = clusterBuilder.Build();
        await Cluster.DeployAsync();
    }

    [OneTimeTearDown]
    public Task TearDown()
    {
        return Cluster.DisposeAsync().AsTask();
    }

    private IClusterClient Client => Cluster.Client;

    [Test]
    [Order(0)]
    public async Task Join_Test()
    {
        // Arrange
        var channel = Client.GetGrain<IChannelGrain>(Guid.NewGuid());
        var nickname = "Boss";

        // Act
        var streamId = await channel.Join(nickname);
        var history = await channel.ReadHistory(10);
    }
    
    [Test]
    [Order(1)]
    public async Task Join_And_Send_Message_Test()
    {
        // Arrange
        var channel = Client.GetGrain<IChannelGrain>(Guid.NewGuid());
        var nickname = "Boss";
        var sentMessage = new ChatMessage(nickname, "Fuck, everyone!", DateTimeOffset.UtcNow);

        // Act
        var streamId = await channel.Join(nickname);
        await channel.SendMessage(sentMessage);
        var history = await channel.ReadHistory(10);

        // Assert
        history.Should().Contain(m => m.Author == sentMessage.Author && m.Text == sentMessage.Text);
    }

    [Test]
    [Order(2)]
    public async Task Leave_Test()
    {
        // Arrange
        var channel = Client.GetGrain<IChannelGrain>(Guid.NewGuid());
        var nickname = "TestUser";
        await channel.Join(nickname);

        // Act
        await channel.Leave(nickname);
        var members = await channel.GetMembers();

        // Assert
        members.Should().NotContain(nickname);
    }

    [Test]
    [Order(3)]
    public async Task Multiple_Clients_Test()
    {
        // Arrange
        var channel = Client.GetGrain<IChannelGrain>(Guid.NewGuid());
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
    [Order(4)]
    public async Task Chat_History_Test()
    {
        // Arrange
        var channel = Client.GetGrain<IChannelGrain>(Guid.NewGuid());
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
}
