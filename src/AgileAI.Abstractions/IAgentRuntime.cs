namespace AgileAI.Abstractions;

public interface IAgentRuntime
{
    Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
}
