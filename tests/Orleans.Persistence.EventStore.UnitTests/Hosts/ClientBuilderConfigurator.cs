using Microsoft.Extensions.Configuration;
using Orleans.TestingHost;

namespace Orleans.Persistence.EventStore.UnitTests.Hosts;

public class ClientBuilderConfigurator : IClientBuilderConfigurator
{
    /// <inheritdoc />
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
    }
}
