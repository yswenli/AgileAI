namespace AgileAI.Abstractions;

public record StreamingChatTurnExecutionContext
{
    public ChatTurnExecutionKind Kind { get; init; }
    public string ModelId { get; init; } = string.Empty;
    public string? Input { get; init; }
    public IReadOnlyList<ChatMessage> Messages { get; init; } = [];
    public ChatOptions? Options { get; set; }
    public string? SessionId { get; init; }
    public string? ConversationId { get; init; }
    public IServiceProvider? ServiceProvider { get; init; }
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
}
