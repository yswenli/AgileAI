namespace AgileAI.Abstractions;

public interface ITool
{
    string Name { get; }
    string? Description { get; }
    object? ParametersSchema { get; }
    Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default);
}
