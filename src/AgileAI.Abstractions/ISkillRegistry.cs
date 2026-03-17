namespace AgileAI.Abstractions;

public interface ISkillRegistry
{
    void Register(ISkill skill);
    void Register(IEnumerable<ISkill> skills);
    bool TryGetSkill(string name, out ISkill? skill);
    IReadOnlyList<ISkill> GetAllSkills();
}
