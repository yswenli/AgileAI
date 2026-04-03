namespace AgileAI.Abstractions;

public record ToolExecutionMiddlewareContext
{
    public ITool Tool { get; init; } = null!;
    public ToolExecutionContext ExecutionContext { get; init; } = null!;
    public IServiceProvider? ServiceProvider { get; init; }
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
}
