namespace AgileAI.Abstractions;

public interface IToolExecutionMiddleware
{
    Task<ToolExecutionOutcome> InvokeAsync(
        ToolExecutionMiddlewareContext context,
        Func<Task<ToolExecutionOutcome>> next,
        CancellationToken cancellationToken = default);
}
