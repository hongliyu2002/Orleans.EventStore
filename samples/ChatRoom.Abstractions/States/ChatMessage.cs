namespace ChatRoom.Abstractions.States;

[Immutable]
[GenerateSerializer]
public record ChatMessage(string Author, string Text, DateTimeOffset Created)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Author} said: [{Text}] at {Created:t}";
    }
}
