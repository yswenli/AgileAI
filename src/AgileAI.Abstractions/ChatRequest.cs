namespace AgileAI.Abstractions;

public record ChatRequest
{
    public string ModelId { get; init; } = string.Empty;
    public IReadOnlyList<ChatMessage> Messages { get; init; } = [];
    public ChatOptions? Options { get; init; }
}
