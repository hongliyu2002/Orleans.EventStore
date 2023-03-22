namespace Orleans.Configuration;

/// <summary>
/// </summary>
public class EventStorePolicyOptions
{
    private TimeSpan? _operationTimeout;

    /// <summary>
    /// </summary>
    public int MaxOperationRetries { get; set; } = 5;

    /// <summary>
    /// </summary>
    public TimeSpan PauseBetweenOperationRetries { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// </summary>
    public TimeSpan OperationTimeout
    {
        get => _operationTimeout ?? TimeSpan.FromMilliseconds(PauseBetweenOperationRetries.TotalMilliseconds * MaxOperationRetries * 6);
        set => SetIfValidTimeout(ref _operationTimeout, value, nameof(OperationTimeout));
    }

    private static void SetIfValidTimeout(ref TimeSpan? field, TimeSpan value, string propertyName)
    {
        if (value <= TimeSpan.Zero && !value.Equals(TimeSpan.FromMilliseconds(-1)))
        {
            throw new ArgumentNullException(propertyName);
        }
        field = value;
    }
}
