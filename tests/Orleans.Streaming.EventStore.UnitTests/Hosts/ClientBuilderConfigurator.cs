using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost;

namespace Orleans.Streaming.EventStore.UnitTests.Hosts;

public class ClientBuilderConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder.Services.AddLogging(builder => builder.AddProvider(new TestOutputLoggerProvider()));
    }
}
