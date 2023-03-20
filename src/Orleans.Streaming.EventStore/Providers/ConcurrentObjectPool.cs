using Microsoft.Extensions.ObjectPool;

namespace Orleans.Streaming.Providers;

internal sealed class ConcurrentObjectPool<T> : ConcurrentObjectPool<T, DefaultConcurrentObjectPoolPolicy<T>>
    where T : class, new()
{
    public ConcurrentObjectPool()
        : base(new DefaultConcurrentObjectPoolPolicy<T>())
    {
    }
}

internal class ConcurrentObjectPool<T, TPoolPolicy> : ObjectPool<T>
    where T : class
    where TPoolPolicy : IPooledObjectPolicy<T>
{
    private readonly ThreadLocal<Stack<T>> _objects = new(() => new Stack<T>());

    private readonly TPoolPolicy _policy;

    public ConcurrentObjectPool(TPoolPolicy policy)
    {
        _policy = policy;
    }

    public int MaxPoolSize { get; set; } = int.MaxValue;

    public override T Get()
    {
        var stack = _objects.Value;
        if (stack != null && stack.TryPop(out var result))
        {
            return result;
        }
        return _policy.Create();
    }

    public override void Return(T obj)
    {
        if (!_policy.Return(obj))
        {
            return;
        }
        var stack = _objects.Value;
        if (stack != null && stack.Count < MaxPoolSize)
        {
            stack.Push(obj);
        }
    }
}

internal readonly struct DefaultConcurrentObjectPoolPolicy<T> : IPooledObjectPolicy<T>
    where T : class, new()
{
    public T Create()
    {
        return new T();
    }

    public bool Return(T obj)
    {
        return true;
    }
}
