using Fluxera.Guards;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Streaming.EventStore.UnitTests.Grains;

public class SubscriberGrain : Grain, ISubscriberGrain
{
    private readonly ILogger<SubscriberGrain> _logger;
    private IAsyncStream<ChatMessage> _stream = null!;
    private IStreamProvider _streamProvider = null!;
    private StreamSubscriptionHandle<ChatMessage>? _streamSubscription;

    /// <inheritdoc />
    public SubscriberGrain(ILogger<SubscriberGrain> logger)
    {
        _logger = Guard.Against.Null(logger, nameof(logger));
    }

    /// <inheritdoc />
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        _streamProvider = this.GetStreamProvider(Constants.StreamProviderName);
    }

    /// <inheritdoc />
    public async Task Subscribe(StreamId streamId)
    {
        _stream = _streamProvider.GetStream<ChatMessage>(streamId);
        _streamSubscription = await _stream.SubscribeAsync(HandleNextAsync, HandleExceptionAsync, HandCompleteAsync);
    }

    /// <inheritdoc />
    public async Task Unsubscribe()
    {
        if (_streamSubscription != null)
        {
            await _streamSubscription.UnsubscribeAsync();
            _streamSubscription = null!;
        }
    }

    protected async Task HandleNextAsync(ChatMessage message, StreamSequenceToken seq)
    {
        var formattedMessage = $"#{seq.SequenceNumber}.{seq.EventIndex} {message.Author} said: [{message.Text}] at {message.Created:t}";
        _logger.LogInformation(formattedMessage);
        await TestContext.Progress.WriteLineAsync(formattedMessage);
    }

    protected async Task HandleExceptionAsync(Exception exception)
    {
        _logger.LogError(exception, exception.Message);
        await TestContext.Progress.WriteLineAsync(exception.Message);
    }

    protected async Task HandCompleteAsync()
    {
        _logger.LogInformation($"Stream {Constants.ChatNamespace} is completed.");
        await TestContext.Progress.WriteLineAsync($"Stream {Constants.ChatNamespace} is completed.");
    }
}
