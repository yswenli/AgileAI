using System.Text;
using System.Text.Json;
using AgileAI.Abstractions;

namespace AgileAI.Studio.Api.Tools;

public class ReadFileTool(WorkspacePathGuard pathGuard) : ITool
{
    private const int MaxCharacters = 12000;

    public string Name => "read_file";

    public string Description => "Read a text file inside the AgileAI workspace.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            path = new { type = "string", description = "Workspace-relative file path to read." }
        },
        required = new[] { "path" }
    };

    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var request = JsonSerializer.Deserialize<ReadFileRequest>(context.ToolCall.Arguments, JsonOptions())
            ?? throw new InvalidOperationException("Invalid read_file arguments.");

        var resolvedPath = pathGuard.ResolvePath(request.Path);
        if (!File.Exists(resolvedPath))
        {
            throw new InvalidOperationException($"File '{request.Path}' was not found.");
        }

        var content = await File.ReadAllTextAsync(resolvedPath, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine($"Path: {pathGuard.ToRelativePath(resolvedPath)}");
        builder.AppendLine();
        if (content.Length > MaxCharacters)
        {
            builder.Append(content[..MaxCharacters]);
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("[Output truncated to 12000 characters]");
        }
        else
        {
            builder.Append(content);
        }

        return new ToolResult
        {
            ToolCallId = context.ToolCall.Id,
            Content = builder.ToString(),
            IsSuccess = true
        };
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

    private sealed record ReadFileRequest(string Path);
}
