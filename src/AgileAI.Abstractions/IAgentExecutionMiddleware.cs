namespace AgileAI.Abstractions;

public interface IAgentExecutionMiddleware
{
    Task<AgentResult> InvokeAsync(
        AgentExecutionContext context,
        Func<Task<AgentResult>> next,
        CancellationToken cancellationToken = default);
}
