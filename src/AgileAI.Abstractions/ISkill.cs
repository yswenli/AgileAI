namespace AgileAI.Abstractions;

public interface ISkill
{
    string Name { get; }
    string? Description { get; }
    SkillManifest? Manifest { get; }
    Task<AgentResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken = default);
}
