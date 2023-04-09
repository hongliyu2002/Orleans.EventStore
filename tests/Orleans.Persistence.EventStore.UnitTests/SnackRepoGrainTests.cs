// using FluentAssertions;
// using Microsoft.Extensions.DependencyInjection;
// using NUnit.Framework;
// using Orleans.Persistence.EventStore.UnitTests.Commands;
// using Orleans.Persistence.EventStore.UnitTests.Grains;
// using Orleans.Persistence.EventStore.UnitTests.Hosts;
// using Orleans.TestingHost;
//
// namespace Orleans.Persistence.EventStore.UnitTests;
//
// [TestFixture]
// public class SnackRepoGrainTests
// {
//     public TestCluster Cluster { get; set; } = null!;
//
//     [OneTimeSetUp]
//     public async Task Setup()
//     {
//         Cluster = new TestClusterBuilder().AddClientBuilderConfigurator<ClientBuilderConfigurator>().AddSiloBuilderConfigurator<SiloConfigurator>().Build();
//         await Cluster.DeployAsync();
//     }
//
//     [OneTimeTearDown]
//     public Task TearDown()
//     {
//         return Cluster.DisposeAsync().AsTask();
//     }
//
//     public Guid _snackId1 = new("11112222-77e6-4ce1-846c-86d28ce34f78");
//     public Guid _snackId2 = new("22223333-77e6-4ce1-846c-86d28ce34f78");
//     public Guid _snackId3 = new("33334444-77e6-4ce1-846c-86d28ce34f78");
//     public Guid _snackId4 = new("44445555-77e6-4ce1-846c-86d28ce34f78");
//     public Guid _snackId5 = new("55556666-77e6-4ce1-846c-86d28ce34f78");
//
//     [Test]
//     [Order(1)]
//     public async Task Should_Create_A_New_SnackGrain()
//     {
//         var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
//         var snackRepoGrain = grainFactory.GetGrain<ISnackCrudRepoGrain>(Guid.Empty);
//         snackRepoGrain.Should().NotBeNull();
//         var createResult1 = await snackRepoGrain.CreateAsync(new SnackRepoCreateCommand(_snackId1, "Apple", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Create_A_New_SnackGrain"));
//         createResult1.IsSuccess.Should().BeTrue();
//     }
//
//     [Test]
//     [Order(2)]
//     public async Task Should_Create_Many_New_SnackGrains()
//     {
//         var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
//         var snackRepoGrain = grainFactory.GetGrain<ISnackCrudRepoGrain>(Guid.Empty);
//         var createResult1 = await snackRepoGrain.CreateAsync(new SnackRepoCreateCommand(_snackId1, "Apple", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Create_Many_New_SnackGrains"));
//         var createResult2 = await snackRepoGrain.CreateAsync(new SnackRepoCreateCommand(_snackId2, "Orange", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Create_Many_New_SnackGrains"));
//         var createResult3 = await snackRepoGrain.CreateAsync(new SnackRepoCreateCommand(_snackId3, "Cafe", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Create_Many_New_SnackGrains"));
//         var createResult4 = await snackRepoGrain.CreateAsync(new SnackRepoCreateCommand(_snackId4, "Coke", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Create_Many_New_SnackGrains"));
//         var createResult5 = await snackRepoGrain.CreateAsync(new SnackRepoCreateCommand(_snackId5, "Banana", Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Create_Many_New_SnackGrains"));
//         createResult1.IsSuccess.Should().BeFalse();
//         createResult2.IsSuccess.Should().BeTrue();
//         createResult3.IsSuccess.Should().BeTrue();
//         createResult4.IsSuccess.Should().BeTrue();
//         createResult5.IsSuccess.Should().BeTrue();
//     }
//
//     [Test]
//     [Order(3)]
//     public async Task Should_Get_SnackGrain()
//     {
//         var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
//         var snackRepoGrain = grainFactory.GetGrain<ISnackCrudRepoGrain>(Guid.Empty);
//         snackRepoGrain.Should().NotBeNull();
//         var getResult1 = await snackRepoGrain.GetAsync(new SnackRepoGetCommand(_snackId1, Guid.NewGuid(), DateTimeOffset.UtcNow, "Should_Get_SnackGrain"));
//         getResult1.IsSuccess.Should().BeTrue();
//         getResult1.Value.Should().NotBeNull();
//         (await getResult1.Value.GetAsync()).Value.Name.Should().Be("Apple");
//     }
//
//     [Test]
//     [Order(4)]
//     public async Task Should_Get_Many_SnackGrains()
//     {
//         var grainFactory = Cluster.ServiceProvider.GetRequiredService<IGrainFactory>();
//         var snackRepoGrain = grainFactory.GetGrain<ISnackCrudRepoGrain>(Guid.Empty);
//         snackRepoGrain.Should().NotBeNull();
//         var getManyResult = await snackRepoGrain.GetManyAsync(new SnackRepoGetManyCommand(new[]
//                                                                                           {
//                                                                                               _snackId1,
//                                                                                               _snackId2,
//                                                                                               _snackId3,
//                                                                                               _snackId4,
//                                                                                               _snackId5
//                                                                                           },
//                                                                                           Guid.NewGuid(),
//                                                                                           DateTimeOffset.UtcNow,
//                                                                                           "Should_Get_Many_SnackGrains"));
//         getManyResult.IsSuccess.Should().BeTrue();
//         getManyResult.Value.Should().HaveCount(5);
//         getManyResult.Value.ForEach(async grain =>
//                                     {
//                                         var snack = await grain.GetAsync();
//                                         await TestContext.Progress.WriteLineAsync(snack.ToString());
//                                     });
//     }
// }
