using System.Text.Json;
using AgileAI.Abstractions;

namespace AgileAI.Extensions.FileSystem;

public class ListDirectoryTool(FileSystemPathGuard pathGuard) : ITool
{
    public string Name => "list_directory";

    public string Description => "List files and directories inside the configured filesystem root.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            path = new { type = "string", description = "Root-relative directory path. Use . for the configured root." }
        },
        required = new[] { "path" }
    };

    public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var request = JsonSerializer.Deserialize<ListDirectoryRequest>(context.ToolCall.Arguments, JsonOptions())
            ?? throw new InvalidOperationException("Invalid list_directory arguments.");

        var resolvedPath = pathGuard.ResolvePath(request.Path);
        if (!Directory.Exists(resolvedPath))
        {
            throw new InvalidOperationException($"Directory '{request.Path}' was not found.");
        }

        var directories = Directory.GetDirectories(resolvedPath)
            .Select(pathGuard.ToRelativePath)
            .Select(path => path + "/");
        var files = Directory.GetFiles(resolvedPath)
            .Select(pathGuard.ToRelativePath);

        var output = string.Join(Environment.NewLine, directories.Concat(files).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(output))
        {
            output = "Directory is empty.";
        }

        return Task.FromResult(new ToolResult
        {
            ToolCallId = context.ToolCall.Id,
            Content = output,
            IsSuccess = true
        });
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

    private sealed record ListDirectoryRequest(string Path);
}
