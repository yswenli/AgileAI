using AgileAI.Abstractions;

namespace AgileAI.Core;

public class RuleBasedSkillPlanner : ISkillPlanner
{
    public Task<SkillPlan> PlanAsync(AgentRequest request, IReadOnlyList<ISkill> skills, CancellationToken cancellationToken = default)
    {
        var input = request.Input ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input) || skills.Count == 0)
        {
            return Task.FromResult(new SkillPlan());
        }

        var allowed = request.AllowedSkills is { Count: > 0 }
            ? new HashSet<string>(request.AllowedSkills, StringComparer.OrdinalIgnoreCase)
            : null;

        ISkill? bestSkill = null;
        var bestScore = 0;

        foreach (var skill in skills)
        {
            if (allowed != null && !allowed.Contains(skill.Name))
            {
                continue;
            }

            var score = ScoreSkill(input, skill);
            if (score > bestScore)
            {
                bestScore = score;
                bestSkill = skill;
            }
        }

        if (bestSkill == null || bestScore < 4)
        {
            return Task.FromResult(new SkillPlan());
        }

        return Task.FromResult(new SkillPlan
        {
            ShouldUseSkill = true,
            SkillName = bestSkill.Name,
            Reason = $"Matched skill '{bestSkill.Name}' with score {bestScore}."
        });
    }

    private static int ScoreSkill(string input, ISkill skill)
    {
        var score = 0;
        var comparison = StringComparison.OrdinalIgnoreCase;

        if (input.Contains(skill.Name, comparison))
        {
            score += 5;
        }

        if (!string.IsNullOrWhiteSpace(skill.Description))
        {
            score += CountKeywordHits(input, skill.Description!) * 2;
        }

        var triggers = skill.Manifest?.Triggers ?? [];
        foreach (var trigger in triggers)
        {
            if (!string.IsNullOrWhiteSpace(trigger) && input.Contains(trigger, comparison))
            {
                score += 4;
            }
        }

        if (triggers.Count(t => !string.IsNullOrWhiteSpace(t) && input.Contains(t, comparison)) > 1)
        {
            score += 2;
        }

        return score;
    }

    private static int CountKeywordHits(string input, string text)
    {
        var count = 0;
        foreach (var token in text.Split(new[] { ' ', ',', '.', ';', ':', '-', '_', '/', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length >= 4 && input.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }
}
