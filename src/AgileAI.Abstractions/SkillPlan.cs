namespace AgileAI.Abstractions;

public record SkillPlan
{
    public bool ShouldUseSkill { get; init; }
    public string? SkillName { get; init; }
    public string? Reason { get; init; }
}
