namespace AgileAI.Abstractions;

public record AgentResult
{
    public string Output { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ChatMessage>? UpdatedHistory { get; init; }
}
