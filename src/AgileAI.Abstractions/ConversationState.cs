namespace AgileAI.Abstractions;

public record ConversationState
{
    public string SessionId { get; init; } = string.Empty;
    public IReadOnlyList<ChatMessage> History { get; init; } = [];
    public string? ActiveSkill { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
