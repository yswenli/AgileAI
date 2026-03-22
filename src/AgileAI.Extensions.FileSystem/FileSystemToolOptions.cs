namespace AgileAI.Extensions.FileSystem;

public class FileSystemToolOptions
{
    public string RootPath { get; set; } = string.Empty;

    public int MaxReadCharacters { get; set; } = 12000;
}
