namespace AgileAI.Abstractions;

public interface ISkill
{
    string Name { get; }
    string? Description { get; }
    Task<AgentResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken = default);
}
