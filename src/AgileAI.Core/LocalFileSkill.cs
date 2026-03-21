using AgileAI.Abstractions;

namespace AgileAI.Core;

public sealed class LocalFileSkill : ISkill
{
    private readonly SkillManifest _manifest;
    private readonly ISkillExecutor _executor;

    public LocalFileSkill(SkillManifest manifest, ISkillExecutor executor)
    {
        _manifest = manifest;
        _executor = executor;
    }

    public string Name => _manifest.Name;
    public string? Description => _manifest.Description;
    public SkillManifest? Manifest => _manifest;

    public Task<AgentResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken = default)
        => _executor.ExecuteAsync(_manifest, context, cancellationToken);
}
