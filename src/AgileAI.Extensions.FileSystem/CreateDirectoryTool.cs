using System.Text.Json;
using AgileAI.Abstractions;

namespace AgileAI.Extensions.FileSystem;

[NeedApproval]
public class CreateDirectoryTool(FileSystemPathGuard pathGuard) : ITool
{
    public string Name => "create_directory";

    public string Description => "Create a directory inside the configured filesystem root. Creates parent directories as needed.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            path = new { type = "string", description = "Root-relative directory path to create. Creates all parent directories as needed." }
        },
        required = new[] { "path" }
    };

    public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var request = JsonSerializer.Deserialize<CreateDirectoryRequest>(context.ToolCall.Arguments, JsonOptions())
            ?? throw new InvalidOperationException("Invalid create_directory arguments.");

        var resolvedPath = pathGuard.ResolvePath(request.Path);
        
        // Create the directory and all parent directories
        Directory.CreateDirectory(resolvedPath);

        var relativePath = pathGuard.ToRelativePath(resolvedPath);
        return Task.FromResult(new ToolResult
        {
            ToolCallId = context.ToolCall.Id,
            Content = $"Created directory: {relativePath}",
            IsSuccess = true
        });
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

    private sealed record CreateDirectoryRequest(string Path);
}
