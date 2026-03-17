namespace AgileAI.Abstractions;

public record ToolResult
{
    public string ToolCallId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsSuccess { get; init; } = true;
}
