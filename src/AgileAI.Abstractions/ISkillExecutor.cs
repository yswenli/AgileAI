namespace AgileAI.Abstractions;

public interface ISkillExecutor
{
    Task<AgentResult> ExecuteAsync(
        SkillManifest manifest,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default);
}
