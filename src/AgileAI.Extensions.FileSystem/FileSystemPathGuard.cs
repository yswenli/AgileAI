namespace AgileAI.Extensions.FileSystem;

public class FileSystemPathGuard(FileSystemToolOptions options)
{
    public string RootPath { get; } = string.IsNullOrWhiteSpace(options.RootPath)
        ? throw new InvalidOperationException("FileSystemToolOptions.RootPath is required.")
        : Path.GetFullPath(options.RootPath);

    public string ResolvePath(string requestedPath)
    {
        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            throw new InvalidOperationException("A root-relative path is required.");
        }

        var sanitized = requestedPath.Replace('/', Path.DirectorySeparatorChar).Trim();
        var combined = Path.IsPathRooted(sanitized)
            ? Path.GetFullPath(sanitized)
            : Path.GetFullPath(Path.Combine(RootPath, sanitized));

        var rootWithSeparator = RootPath.EndsWith(Path.DirectorySeparatorChar)
            ? RootPath
            : RootPath + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(combined, RootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path escapes the configured filesystem root and is not allowed.");
        }

        return combined;
    }

    public string ToRelativePath(string fullPath)
        => Path.GetRelativePath(RootPath, fullPath).Replace(Path.DirectorySeparatorChar, '/');
}
