namespace AgileAI.Abstractions;

public record SkillExecutionContext
{
    public AgentRequest Request { get; init; } = null!;
    public IServiceProvider? ServiceProvider { get; init; }
}
