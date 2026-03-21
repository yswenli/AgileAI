using AgileAI.Abstractions;
using AgileAI.Core;

namespace AgileAI.Tests;

public class DefaultSkillContinuationPolicyTests
{
    [Fact]
    public async Task DecideAsync_WithActiveSkill_ShouldContinue()
    {
        var policy = new DefaultSkillContinuationPolicy();
        var skill = new LocalFileSkill(new SkillManifest { Name = "weather" }, new NoopSkillExecutor());

        var decision = await policy.DecideAsync(
            new AgentRequest { Input = "continue", EnableSkills = true },
            new ConversationState { SessionId = "s1", ActiveSkill = "weather" },
            [skill]);

        Assert.True(decision.ContinueActiveSkill);
        Assert.Equal("weather", decision.SkillName);
    }

    [Fact]
    public async Task DecideAsync_WithPreferredSkillSwitch_ShouldNotContinue()
    {
        var policy = new DefaultSkillContinuationPolicy();
        var skill = new LocalFileSkill(new SkillManifest { Name = "weather" }, new NoopSkillExecutor());

        var decision = await policy.DecideAsync(
            new AgentRequest { Input = "switch", EnableSkills = true, PreferredSkill = "calendar" },
            new ConversationState { SessionId = "s1", ActiveSkill = "weather" },
            [skill]);

        Assert.False(decision.ContinueActiveSkill);
    }

    private sealed class NoopSkillExecutor : ISkillExecutor
    {
        public Task<AgentResult> ExecuteAsync(SkillManifest manifest, SkillExecutionContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult { IsSuccess = true });
    }
}
