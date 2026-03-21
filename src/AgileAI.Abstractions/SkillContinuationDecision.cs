namespace AgileAI.Abstractions;

public record SkillContinuationDecision
{
    public bool ContinueActiveSkill { get; init; }
    public string? SkillName { get; init; }
    public string? Reason { get; init; }

    public static SkillContinuationDecision Continue(string skillName, string? reason = null) => new()
    {
        ContinueActiveSkill = true,
        SkillName = skillName,
        Reason = reason
    };

    public static SkillContinuationDecision NoContinuation(string? reason = null) => new()
    {
        ContinueActiveSkill = false,
        Reason = reason
    };
}
