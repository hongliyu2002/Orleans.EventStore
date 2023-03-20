namespace Orleans.Streaming.EventStore.UnitTests.Grains;

[Immutable]
[GenerateSerializer]
public record ChatMessage(string Author, string Text, DateTimeOffset Created);
