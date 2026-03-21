namespace AgileAI.Abstractions;

public interface ISkillPlanner
{
    Task<SkillPlan> PlanAsync(
        AgentRequest request,
        IReadOnlyList<ISkill> skills,
        CancellationToken cancellationToken = default);
}
