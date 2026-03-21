using AgileAI.Abstractions;

namespace AgileAI.Core;

public static class SkillPromptHelper
{
    public const string MarkerPrefix = "[AgileAI Skill Prompt]";

    public static string BuildSystemPrompt(SkillManifest manifest)
    {
        return $"""
{MarkerPrefix} Name={manifest.Name}
You are executing the AgileAI local skill: {manifest.Name}

Skill name: {manifest.Name}
Skill description: {manifest.Description}
Skill version: {manifest.Version}
Skill entry mode: {manifest.EntryMode}
Skill root directory: {manifest.RootDirectory}
Skill markdown path: {manifest.SkillMarkdownPath}

Skill instructions:
{manifest.InstructionBody}

Use these instructions as task-specific behavior. If the skill references relative files, resolve them relative to the skill root directory.
""";
    }

    public static bool IsGeneratedSkillPrompt(ChatMessage message)
    {
        return message.Role == ChatRole.System &&
               message.TextContent?.StartsWith(MarkerPrefix, StringComparison.Ordinal) == true;
    }

    public static bool IsGeneratedSkillPromptFor(ChatMessage message, string skillName)
    {
        return message.Role == ChatRole.System &&
               message.TextContent?.StartsWith($"{MarkerPrefix} Name={skillName}", StringComparison.Ordinal) == true;
    }

    public static IReadOnlyList<ChatMessage> PrepareHistoryForSkill(IReadOnlyList<ChatMessage>? history, SkillManifest manifest)
    {
        var filtered = (history ?? [])
            .Where(m => !IsGeneratedSkillPrompt(m))
            .ToList();

        filtered.Insert(0, ChatMessage.System(BuildSystemPrompt(manifest)));
        return filtered;
    }
}
