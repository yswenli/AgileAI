namespace AgileAI.Abstractions;

public interface ISkillContinuationPolicy
{
    Task<SkillContinuationDecision> DecideAsync(
        AgentRequest request,
        ConversationState? state,
        IReadOnlyList<ISkill> skills,
        CancellationToken cancellationToken = default);
}
