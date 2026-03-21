using AgileAI.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgileAI.Core;

public class DefaultSkillContinuationPolicy : ISkillContinuationPolicy
{
    private readonly ILogger<DefaultSkillContinuationPolicy>? _logger;

    public DefaultSkillContinuationPolicy(ILogger<DefaultSkillContinuationPolicy>? logger = null)
    {
        _logger = logger;
    }

    public Task<SkillContinuationDecision> DecideAsync(
        AgentRequest request,
        ConversationState? state,
        IReadOnlyList<ISkill> skills,
        CancellationToken cancellationToken = default)
    {
        if (state == null || string.IsNullOrWhiteSpace(state.ActiveSkill))
        {
            var decision = SkillContinuationDecision.NoContinuation("No active skill in session state.");
            _logger?.LogDebug("Continuation decision: {Reason}", decision.Reason);
            return Task.FromResult(decision);
        }

        if (!request.EnableSkills)
        {
            var decision = SkillContinuationDecision.NoContinuation("Skills disabled for request.");
            _logger?.LogDebug("Continuation decision: {Reason}", decision.Reason);
            return Task.FromResult(decision);
        }

        if (!string.IsNullOrWhiteSpace(request.PreferredSkill) &&
            !string.Equals(request.PreferredSkill, state.ActiveSkill, StringComparison.Ordinal))
        {
            var decision = SkillContinuationDecision.NoContinuation("Preferred skill overrides active skill.");
            _logger?.LogInformation(
                "Continuation skipped. PreferredSkill={PreferredSkill}, ActiveSkill={ActiveSkill}, Reason={Reason}",
                request.PreferredSkill,
                state.ActiveSkill,
                decision.Reason);
            return Task.FromResult(decision);
        }

        var hasActiveSkill = skills.Any(s => string.Equals(s.Name, state.ActiveSkill, StringComparison.Ordinal));
        if (!hasActiveSkill)
        {
            var decision = SkillContinuationDecision.NoContinuation("Active skill is not registered.");
            _logger?.LogWarning("Continuation skipped. ActiveSkill={ActiveSkill}, Reason={Reason}", state.ActiveSkill, decision.Reason);
            return Task.FromResult(decision);
        }

        var continueDecision = SkillContinuationDecision.Continue(state.ActiveSkill, "Continuing active skill from session state.");
        _logger?.LogInformation("Continuation enabled. ActiveSkill={ActiveSkill}", state.ActiveSkill);
        return Task.FromResult(continueDecision);
    }
}
