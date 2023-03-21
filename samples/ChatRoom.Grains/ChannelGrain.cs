using ChatRoom.Abstractions;
using ChatRoom.Abstractions.States;
using Orleans.Runtime;
using Orleans.Streams;

namespace ChatRoom.Grains;

public class ChannelGrain : Grain, IChannelGrain
{
    private IStreamProvider _streamProvider = null!;
    private IAsyncStream<ChatMessage> _stream = null!;

    private readonly List<string> _members = new(8);
    private readonly List<ChatMessage> _messages = new(128);

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        _streamProvider = this.GetStreamProvider(Constants.StreamProviderName);
        _stream = _streamProvider.GetStream<ChatMessage>(StreamId.Create(Constants.ChatNamespace, this.GetPrimaryKey()));
    }

    /// <inheritdoc />
    public async Task<StreamId> Join(string nickname)
    {
        _members.Add(nickname);
        await _stream.OnNextAsync(new ChatMessage("System", $"{nickname} joins the chat '{this.GetPrimaryKeyString()}' ...", DateTimeOffset.UtcNow));
        return _stream.StreamId;
    }

    /// <inheritdoc />
    public async Task<StreamId> Leave(string nickname)
    {
        _members.Remove(nickname);
        await _stream.OnNextAsync(new ChatMessage("System", $"{nickname} leaves the chat...", DateTimeOffset.UtcNow));
        return _stream.StreamId;
    }

    /// <inheritdoc />
    public async Task<bool> SendMessage(ChatMessage message)
    {
        _messages.Add(message);
        await _stream.OnNextAsync(message);
        return true;
    }

    /// <inheritdoc />
    public Task<ChatMessage[]> ReadHistory(int maxCount)
    {
        var messages = _messages.OrderByDescending(message => message.Created).Take(maxCount).OrderBy(message => message.Created).ToArray();
        return Task.FromResult(messages);
    }

    /// <inheritdoc />
    public Task<string[]> GetMembers()
    {
        return Task.FromResult(_members.ToArray());
    }
}
