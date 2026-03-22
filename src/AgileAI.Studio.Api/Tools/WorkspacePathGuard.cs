namespace AgileAI.Studio.Api.Tools;

public class WorkspacePathGuard(IHostEnvironment hostEnvironment)
{
    public string WorkspaceRoot { get; } = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, "..", ".."));

    public string ResolvePath(string requestedPath)
    {
        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            throw new InvalidOperationException("A workspace-relative path is required.");
        }

        var sanitized = requestedPath.Replace('/', Path.DirectorySeparatorChar).Trim();
        var combined = Path.IsPathRooted(sanitized)
            ? Path.GetFullPath(sanitized)
            : Path.GetFullPath(Path.Combine(WorkspaceRoot, sanitized));

        var rootWithSeparator = WorkspaceRoot.EndsWith(Path.DirectorySeparatorChar)
            ? WorkspaceRoot
            : WorkspaceRoot + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(combined, WorkspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path escapes the Studio workspace and is not allowed.");
        }

        return combined;
    }

    public string ToRelativePath(string fullPath)
        => Path.GetRelativePath(WorkspaceRoot, fullPath).Replace(Path.DirectorySeparatorChar, '/');
}
