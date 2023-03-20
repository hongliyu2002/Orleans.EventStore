using EventStore.Client;
using Microsoft.Extensions.ObjectPool;

namespace Orleans.Streaming.Providers;

/// <summary>
///     Pool for <see cref="List{ResolvedEvent}" /> objects.
/// </summary>
internal class ResolvedEventsPool
{
    private readonly ObjectPool<List<ResolvedEvent>> _eventsPool;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResolvedEventsPool" /> class.
    /// </summary>
    public ResolvedEventsPool()
    {
        _eventsPool = new ConcurrentObjectPool<List<ResolvedEvent>>();
    }

    /// <summary>
    ///     Gets a list of resolved event from the pool.
    /// </summary>
    public List<ResolvedEvent> GetList()
    {
        return _eventsPool.Get();
    }

    /// <summary>
    ///     Returns a list of resolved event to the pool.
    /// </summary>
    public void ReturnList(List<ResolvedEvent> events)
    {
        events.Clear();
        _eventsPool.Return(events);
    }
}
