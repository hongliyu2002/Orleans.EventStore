using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Configuration;

/// <summary>
/// </summary>
public class StreamCheckpointerConfigurationValidator : IConfigurationValidator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string name;

    /// <summary>
    ///     Validates the configuration of a stream checkpointer by checking that a corresponding <see cref="IStreamQueueCheckpointerFactory" /> is configured with the specified stream provider name.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="_name">The name of the stream provider to validate.</param>
    public StreamCheckpointerConfigurationValidator(IServiceProvider serviceProvider, string _name)
    {
        _serviceProvider = serviceProvider;
        name = _name;
    }

    /// <summary>
    ///     Validates the configuration of the stream checkpointer.
    /// </summary>
    /// <exception cref="OrleansConfigurationException">Thrown if no IStreamQueueCheckpointer is configured with the PersistentStreamProvider.</exception>
    public void ValidateConfiguration()
    {
        var checkpointerFactory = _serviceProvider.GetServiceByName<IStreamQueueCheckpointerFactory>(name);
        if (checkpointerFactory == null)
        {
            throw new OrleansConfigurationException($"No IStreamQueueCheckpointer is configured with PersistentStreamProvider {name}. Please configure one.");
        }
    }
}
