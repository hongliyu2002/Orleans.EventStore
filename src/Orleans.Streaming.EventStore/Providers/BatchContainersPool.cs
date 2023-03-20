using Microsoft.Extensions.ObjectPool;
using Orleans.Streams;

namespace Orleans.Streaming.Providers;

/// <summary>
///     Pool for <see cref="List{IBatchContainer}" /> objects.
/// </summary>
internal class BatchContainersPool
{
    private readonly ObjectPool<List<IBatchContainer>> _containersPool;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BatchContainersPool" /> class.
    /// </summary>
    public BatchContainersPool()
    {
        _containersPool = new ConcurrentObjectPool<List<IBatchContainer>>();
    }

    /// <summary>
    ///     Gets a list of batch container from the pool.
    /// </summary>
    public List<IBatchContainer> GetList()
    {
        return _containersPool.Get();
    }

    /// <summary>
    ///     Returns a list of batch container to the pool.
    /// </summary>
    public void ReturnList(List<IBatchContainer> containers)
    {
        containers.Clear();
        _containersPool.Return(containers);
    }
}
