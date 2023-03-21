using ChatRoom.Abstractions.States;
using Orleans.Runtime;

namespace ChatRoom.Abstractions;

public interface IChannelGrain : IGrainWithStringKey
{
    Task<StreamId> Join(string nickname);

    Task<StreamId> Leave(string nickname);

    Task<bool> SendMessage(ChatMessage message);

    Task<ChatMessage[]> ReadHistory(int maxCount);

    Task<string[]> GetMembers();
}
