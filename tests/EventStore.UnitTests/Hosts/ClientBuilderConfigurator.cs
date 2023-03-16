using Microsoft.Extensions.Configuration;
using Orleans.TestingHost;

namespace EventStore.UnitTests.Hosts;

public class ClientBuilderConfigurator : IClientBuilderConfigurator
{
    /// <inheritdoc />
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
    }
}
