namespace AgileAI.Abstractions;

public record ToolApprovalRequest
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string ToolCallId { get; init; } = string.Empty;
    public string ToolName { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
    public string? SessionId { get; init; }
    public string? ConversationId { get; init; }
    public IReadOnlyList<ChatMessage> ChatHistory { get; init; } = [];
    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string? Reason { get; init; }
}
