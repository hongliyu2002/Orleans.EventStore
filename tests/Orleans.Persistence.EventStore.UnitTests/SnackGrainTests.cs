using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orleans.Persistence.EventStore.UnitTests.Commands;
using Orleans.Persistence.EventStore.UnitTests.Grains;
using Orleans.Persistence.EventStore.UnitTests.Hosts;
using Orleans.TestingHost;

namespace Orleans.Persistence.EventStore.UnitTests;

[TestFixture]
public class SnackGrainTests
{
    public TestCluster Cluster { get; set; } = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        Cluster = new TestClusterBuilder().AddClientBuilderConfigurator<ClientBuilderConfigurator>().AddSiloBuilderConfigurator<SiloConfigurator>().Build();
        await Cluster.DeployAsync();
    }

    [OneTimeTearDown]
    public Task TearDown()
    {
        return Cluster.DisposeAsync().AsTask();
    }

    public Guid _snackId = new("11112222-77e6-4ce1-846c-86d28ce34f78");

    [Test]
    [Order(1)]
    public async Task Should_Create_A_New_Snack()
    {
        var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
        var snackGrain = grainFactory.GetGrain<ISnackGrain>(_snackId);
        snackGrain.Should().NotBeNull();
        var canInitialize = await snackGrain.CanInitializeAsync();
        canInitialize.Should().BeTrue();
        var initializeResult = await snackGrain.InitializeAsync(new SnackInitializeCommand("Apple", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Create_A_New_Snack"));
        initializeResult.IsSuccess.Should().BeTrue();
    }

    [Test]
    [Order(2)]
    public async Task Should_Change_Name_Of_Snack()
    {
        var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
        var snackGrain = grainFactory.GetGrain<ISnackGrain>(_snackId);
        snackGrain.Should().NotBeNull();
        var canChangeName = await snackGrain.CanChangeNameAsync();
        canChangeName.Should().BeTrue();
        var changeNameResult = await snackGrain.ChangeNameAsync(new SnackChangeNameCommand("Orange", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Change_Name_Of_Snack"));
        changeNameResult.IsSuccess.Should().BeTrue();
    }

    [Test]
    [Order(3)]
    public async Task Should_Remove_SnackG()
    {
        var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
        var snackGrain = grainFactory.GetGrain<ISnackGrain>(_snackId);
        snackGrain.Should().NotBeNull();
        var canChangeName = await snackGrain.CanRemoveAsync();
        canChangeName.Should().BeTrue();
        var removeResult = await snackGrain.RemoveAsync(new SnackRemoveCommand(Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Remove_Snack"));
        removeResult.IsSuccess.Should().BeTrue();
    }

    [Test]
    [Order(4)]
    public async Task Should_Get_Snack()
    {
        var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
        var snackGrain = grainFactory.GetGrain<ISnackGrain>(_snackId);
        snackGrain.Should().NotBeNull();
        var getResult = await snackGrain.GetAsync();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Name.Should().Be("Orange");
        await TestContext.Progress.WriteLineAsync(getResult.Value.ToString());
    }
}
