namespace AgileAI.Abstractions;

public record SkillManifest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Version { get; init; }
    public string? EntryMode { get; init; }
    public string RootDirectory { get; init; } = string.Empty;
    public string SkillMarkdownPath { get; init; } = string.Empty;
    public IReadOnlyList<string> Triggers { get; init; } = [];
    public IReadOnlyList<string> Files { get; init; } = [];
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    public string InstructionBody { get; init; } = string.Empty;
}
