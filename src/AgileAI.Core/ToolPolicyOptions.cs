namespace AgileAI.Core;

public sealed class ToolPolicyOptions
{
    public IReadOnlyCollection<string>? AllowedToolNames { get; set; }
    public IReadOnlyCollection<string>? DeniedToolNames { get; set; }
    public string? DenialMessage { get; set; }
}
