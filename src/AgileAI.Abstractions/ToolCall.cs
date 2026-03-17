namespace AgileAI.Abstractions;

public record ToolCall
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
}
