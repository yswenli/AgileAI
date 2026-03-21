using System.Text.RegularExpressions;
using AgileAI.Abstractions;

namespace AgileAI.Core;

public class LocalFileSkillLoader : ILocalSkillLoader
{
    private readonly LocalSkillsOptions _options;

    public LocalFileSkillLoader(LocalSkillsOptions? options = null)
    {
        _options = options ?? new LocalSkillsOptions();
    }

    public async Task<IReadOnlyList<SkillManifest>> LoadFromDirectoryAsync(string rootDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return [];
        }

        var manifests = new List<SkillManifest>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in Directory.GetDirectories(rootDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var skillFile = Path.Combine(dir, "SKILL.md");
            if (!File.Exists(skillFile))
            {
                continue;
            }

            try
            {
                var manifest = await ParseManifestAsync(dir, skillFile, cancellationToken);
                if (!names.Add(manifest.Name))
                {
                    if (_options.ThrowOnDuplicateSkill)
                    {
                        throw new InvalidOperationException($"Duplicate skill name '{manifest.Name}'.");
                    }

                    continue;
                }

                manifests.Add(manifest);
            }
            catch when (_options.IgnoreInvalidSkills)
            {
            }
        }

        return manifests;
    }

    private static async Task<SkillManifest> ParseManifestAsync(string skillRoot, string skillFile, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(skillFile, cancellationToken);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var body = content;

        if (content.StartsWith("---\n") || content.StartsWith("---\r\n"))
        {
            var match = Regex.Match(content, "^---\\r?\\n(?<meta>.*?)\\r?\\n---\\r?\\n(?<body>[\\s\\S]*)$", RegexOptions.Singleline);
            if (match.Success)
            {
                body = match.Groups["body"].Value.Trim();
                foreach (var line in match.Groups["meta"].Value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var idx = line.IndexOf(':');
                    if (idx <= 0) continue;
                    var key = line[..idx].Trim();
                    var value = line[(idx + 1)..].Trim();
                    metadata[key] = value;
                }
            }
        }

        var triggers = ExtractList(content, "triggers");
        var files = ExtractList(content, "files");

        return new SkillManifest
        {
            Name = metadata.TryGetValue("name", out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : new DirectoryInfo(skillRoot).Name,
            Description = metadata.GetValueOrDefault("description"),
            Version = metadata.GetValueOrDefault("version"),
            EntryMode = metadata.GetValueOrDefault("entry") ?? "prompt",
            RootDirectory = skillRoot,
            SkillMarkdownPath = skillFile,
            Triggers = triggers,
            Files = files,
            Metadata = metadata,
            InstructionBody = body
        };
    }

    private static IReadOnlyList<string> ExtractList(string content, string key)
    {
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var values = new List<string>();
        var inList = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (line.StartsWith($"{key}:", StringComparison.OrdinalIgnoreCase))
            {
                inList = true;
                continue;
            }

            if (inList)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("- "))
                {
                    values.Add(trimmed[2..].Trim());
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    break;
                }
            }
        }

        return values;
    }
}
