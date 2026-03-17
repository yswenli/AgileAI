namespace AgileAI.Abstractions;

public record ChatResponse
{
    public ChatMessage? Message { get; init; }
    public UsageInfo? Usage { get; init; }
    public string? FinishReason { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
