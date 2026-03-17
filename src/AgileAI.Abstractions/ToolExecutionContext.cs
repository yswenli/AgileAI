namespace AgileAI.Abstractions;

public record ToolExecutionContext
{
    public ToolCall ToolCall { get; init; } = null!;
    public IReadOnlyList<ChatMessage> ChatHistory { get; init; } = [];
    public IServiceProvider? ServiceProvider { get; init; }
}
