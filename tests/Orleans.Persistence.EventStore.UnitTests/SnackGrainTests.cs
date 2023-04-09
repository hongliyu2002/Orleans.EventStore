using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orleans.Persistence.EventStore.UnitTests.Hosts;
using Orleans.TestingHost;
using Vending.Domain.Abstractions.Snacks;

namespace Orleans.Persistence.EventStore.UnitTests;

[TestFixture]
public class SnackGrainTests
{
    public TestCluster Cluster { get; set; } = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var builder = new TestClusterBuilder().AddClientBuilderConfigurator<ClientBuilderConfigurator>().AddSiloBuilderConfigurator<SiloConfigurator>();
        builder.Options.ServiceId = "VendingService";
        builder.Options.ServiceId = "VendingCluster";
        // builder.Options.InitialSilosCount = 1;
        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    [OneTimeTearDown]
    public Task TearDown()
    {
        return Cluster.DisposeAsync().AsTask();
    }

    public Guid _snackId = new("11114444-77e6-4ce1-846c-86d28ce34f78");

    [Test]
    [Order(1)]
    public async Task Should_Create_A_New_Snack()
    {
        var command = new SnackInitializeCommand(Guid.NewGuid(), "Apple", null, Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Create_A_New_Snack");
        var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
        var snackGrain = grainFactory.GetGrain<ISnackGrain>(_snackId);
        snackGrain.Should().NotBeNull();
        var canInitialize = await snackGrain.CanInitializeAsync(command);
        canInitialize.Should().BeTrue();
        var initializeResult = await snackGrain.InitializeAsync(command);
        initializeResult.IsSuccess.Should().BeTrue();
    }

    [Test]
    [Order(2)]
    public async Task Should_Change_Name_Of_Snack()
    {
        var command = new SnackUpdateCommand("Orange", null, Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Change_Name_Of_Snack");
        var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
        var snackGrain = grainFactory.GetGrain<ISnackGrain>(_snackId);
        snackGrain.Should().NotBeNull();
        var canChangeName = await snackGrain.CanUpdateAsync(command);
        canChangeName.Should().BeTrue();
        var changeNameResult = await snackGrain.UpdateAsync(command);
        changeNameResult.IsSuccess.Should().BeTrue();
    }
    //
    // [Test]
    // [Order(3)]
    // public async Task Should_Remove_SnackG()
    // {
    //     var command = new SnackDeleteCommand(Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Remove_Snack");
    //     var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
    //     var snackGrain = grainFactory.GetGrain<ISnackGrain>(_snackId);
    //     snackGrain.Should().NotBeNull();
    //     var canChangeName = await snackGrain.CanDeleteAsync(command);
    //     canChangeName.Should().BeTrue();
    //     var removeResult = await snackGrain.DeleteAsync(command);
    //     removeResult.IsSuccess.Should().BeTrue();
    // }

    [Test]
    [Order(4)]
    public async Task Should_Get_Snack()
    {
        var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
        var snackGrain = grainFactory.GetGrain<ISnackGrain>(_snackId);
        snackGrain.Should().NotBeNull();
        var getResult = await snackGrain.GetSnackAsync();
        getResult.Name.Should().Be("Orange");
        await TestContext.Progress.WriteLineAsync(getResult.ToString());
    }
}