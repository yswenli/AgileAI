namespace AgileAI.Abstractions;

public record ToolExecutionOutcome
{
    public ToolResult Result { get; init; } = null!;
    public ToolApprovalRequest? PendingApprovalRequest { get; init; }
}
