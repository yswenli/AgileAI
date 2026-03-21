namespace AgileAI.Core;

public class LocalSkillsOptions
{
    public string RootDirectory { get; set; } = "skills";
    public bool ThrowOnDuplicateSkill { get; set; } = true;
    public bool IgnoreInvalidSkills { get; set; } = false;
}
