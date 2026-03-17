namespace AgileAI.Abstractions;

public record UsageInfo
{
    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public int? TotalTokens { get; init; }
}
