using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Streaming.EventStore.UnitTests.Grains;

public class ChannelGrain : Grain, IChannelGrain
{
    private readonly string _provider = Constants.StreamProviderName;
    private readonly string _namespace = Constants.ChatNamespace;

    private IStreamProvider _streamProvider = null!;
    private IAsyncStream<ChatMessage> _stream = null!;

    private readonly List<ChatMessage> _messages = new(128);
    private readonly List<string> _onlineMembers = new(8);

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        _streamProvider = this.GetStreamProvider(_provider);
        _stream = _streamProvider.GetStream<ChatMessage>(StreamId.Create(_namespace, this.GetPrimaryKey()));
    }

    public async Task<StreamId> Join(string nickname)
    {
        _onlineMembers.Add(nickname);
        await _stream.OnNextAsync(new ChatMessage("System", $"{nickname} joins the chat '{this.GetPrimaryKeyString()}' ...", DateTimeOffset.UtcNow));
        return _stream.StreamId;
    }

    public async Task<StreamId> Leave(string nickname)
    {
        _onlineMembers.Remove(nickname);
        await _stream.OnNextAsync(new ChatMessage("System", $"{nickname} leaves the chat...", DateTimeOffset.UtcNow));
        return _stream.StreamId;
    }

    public async Task<bool> SendMessage(ChatMessage message)
    {
        _messages.Add(message);
        await _stream.OnNextAsync(message);
        return true;
    }

    public Task<ChatMessage[]> ReadHistory(int numberOfMessages)
    {
        var messages = _messages.OrderByDescending(x => x.Created).Take(numberOfMessages).OrderBy(x => x.Created).ToArray();
        return Task.FromResult(messages);
    }

    public Task<string[]> GetMembers()
    {
        return Task.FromResult(_onlineMembers.ToArray());
    }
}
