using Orleans.Configuration;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public static class SiloEventStoreQueueStreamConfiguratorExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="cacheSize"></param>
    public static void ConfigureCacheSize(this ISiloEventStoreQueueStreamConfigurator configurator, int cacheSize = SimpleQueueCacheOptions.DEFAULT_CACHE_SIZE)
    {
        configurator.Configure<SimpleQueueCacheOptions>(optionsBuilder =>
                                                        {
                                                            optionsBuilder.Configure(options => options.CacheSize = cacheSize);
                                                        });
    }
}
