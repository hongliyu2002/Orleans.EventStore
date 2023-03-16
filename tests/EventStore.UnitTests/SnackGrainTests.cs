using EventStore.UnitTests.Commands;
using EventStore.UnitTests.Grains;
using EventStore.UnitTests.Hosts;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orleans.TestingHost;

namespace EventStore.UnitTests;

[TestFixture]
public class SnackGrainTests
{
    public TestCluster Cluster { get; set; }

    [OneTimeSetUp]
    public async Task Setup()
    {
        Cluster = new TestClusterBuilder().AddClientBuilderConfigurator<ClientBuilderConfigurator>()
                                          .AddSiloBuilderConfigurator<SiloConfigurator>()
                                          .Build();
        await Cluster.DeployAsync();
    }
    
    [OneTimeTearDown]
    public Task TearDown()
    {
        return Cluster.DisposeAsync().AsTask();
    }
    
    public Guid _snackId= new Guid("11112222-77e6-4ce1-846c-86d28ce34f78");
    
    [Test]
    public async Task Should_Create_A_New_SnackGrain()
    {
        var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
        var snackGrain = grainFactory.GetGrain<ISnackGrain>(_snackId);
        snackGrain.Should().NotBeNull();
        var canInitialize = await snackGrain.CanInitializeAsync();
        canInitialize.Should().BeTrue();
        var initializeResult = await snackGrain.InitializeAsync(new SnackInitializeCommand("Apple", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Create_A_New_SnackGrain"));
        initializeResult.IsSuccess.Should()
                        .BeTrue();
    }
}
