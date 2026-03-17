using AgileAI.Abstractions;

namespace AgileAI.Core;

public class InMemorySkillRegistry : ISkillRegistry
{
    private readonly Dictionary<string, ISkill> _skills = new();

    public void Register(ISkill skill)
    {
        _skills[skill.Name] = skill;
    }

    public void Register(IEnumerable<ISkill> skills)
    {
        foreach (var skill in skills)
        {
            Register(skill);
        }
    }

    public bool TryGetSkill(string name, out ISkill? skill)
    {
        return _skills.TryGetValue(name, out skill);
    }

    public IReadOnlyList<ISkill> GetAllSkills()
    {
        return _skills.Values.ToList();
    }
}
