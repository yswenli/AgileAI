namespace AgileAI.Abstractions;

public record AgentExecutionContext
{
    public AgentRequest OriginalRequest { get; init; } = null!;
    public AgentRequest Request { get; init; } = null!;
    public string ModelId { get; init; } = string.Empty;
    public ConversationState? SessionState { get; init; }
    public IServiceProvider? ServiceProvider { get; init; }
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
}
