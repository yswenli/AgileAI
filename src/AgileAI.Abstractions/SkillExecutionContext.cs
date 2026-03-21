namespace AgileAI.Abstractions;

public record SkillExecutionContext
{
    public AgentRequest Request { get; init; } = null!;
    public IServiceProvider? ServiceProvider { get; init; }
    public string? ModelId { get; init; }
    public IReadOnlyList<ChatMessage>? History { get; init; }
    public string? SkillRootDirectory { get; init; }
    public string? SkillMarkdownPath { get; init; }
    public IDictionary<string, object?> Items { get; init; } = new Dictionary<string, object?>();
}
