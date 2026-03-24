using AgileAI.Abstractions;
using AgileAI.Extensions.FileSystem;

namespace AgileAI.Tests;

public class MoveFileToolTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FileSystemPathGuard _pathGuard;
    private readonly MoveFileTool _tool;


    public MoveFileToolTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"agileai-move-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);


        var options = new FileSystemToolOptions { RootPath = _tempRoot };
        _pathGuard = new FileSystemPathGuard(options);
        _tool = new MoveFileTool(_pathGuard);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task ExecuteAsync_WithValidFilePath_MovesFile()
    {
        // Arrange
        var sourcePath = "source.txt";
        var destPath = "dest.txt";
        var fullSource = Path.Combine(_tempRoot, sourcePath);
        var fullDest = Path.Combine(_tempRoot, destPath);
        await File.WriteAllTextAsync(fullSource, "test content");

        var toolCall = new ToolCall
        {
            Id = "test-1",
            Name = _tool.Name,
            Arguments = $"{{\"source_path\":\"{sourcePath}\",\"destination_path\":\"{destPath}\"}}"
        };
        var context = new ToolExecutionContext(toolCall);


        // Act
        var result = await _tool.ExecuteAsync(context);


        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(File.Exists(fullSource));
        Assert.True(File.Exists(fullDest));
        Assert.Equal("test content", await File.ReadAllTextAsync(fullDest));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidDirectoryPath_MovesDirectory()
    {
        // Arrange
        var sourcePath = "source-dir";
        var destPath = "dest-dir";
        var fullSource = Path.Combine(_tempRoot, sourcePath);
        var fullDest = Path.Combine(_tempRoot, destPath);
        Directory.CreateDirectory(fullSource);
        await File.WriteAllTextAsync(Path.Combine(fullSource, "file.txt"), "content");
        var toolCall = new ToolCall
        {
            Id = "test-2",
            Name = _tool.Name,
            Arguments = $"{{\"source_path\":\"{sourcePath}\",\"destination_path\":\"{destPath}\"}}"
        };
        var context = new ToolExecutionContext(toolCall);


        // Act
        var result = await _tool.ExecuteAsync(context);


        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(Directory.Exists(fullSource));
        Assert.True(Directory.Exists(fullDest));
        Assert.True(File.Exists(Path.Combine(fullDest, "file.txt")));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentSource_ThrowsException()
    {
        // Arrange
        var sourcePath = "non-existent.txt";
        var destPath = "dest.txt";
        var toolCall = new ToolCall
        {
            Id = "test-3",
            Name = _tool.Name,
            Arguments = $"{{\"source_path\":\"{sourcePath}\",\"destination_path\":\"{destPath}\"}}"
        };
        var context = new ToolExecutionContext(toolCall);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _tool.ExecuteAsync(context));
        Assert.Contains("does not exist", ex.Message);
    }
}
