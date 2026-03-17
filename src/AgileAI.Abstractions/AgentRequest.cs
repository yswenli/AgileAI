namespace AgileAI.Abstractions;

public record AgentRequest
{
    public string Input { get; init; } = string.Empty;
    public string? ModelId { get; init; }
    public IReadOnlyList<ChatMessage>? History { get; init; }
}
