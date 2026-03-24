using AgileAI.Abstractions;
using AgileAI.Extensions.FileSystem;

namespace AgileAI.Tests;

public class CreateDirectoryToolTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FileSystemPathGuard _pathGuard;
    private readonly CreateDirectoryTool _tool;

    public CreateDirectoryToolTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"agileai-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempRoot);

        var options = new FileSystemToolOptions { RootPath = _tempRoot };
        _pathGuard = new FileSystemPathGuard(options);
        _tool = new CreateDirectoryTool(_pathGuard);
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
    public async Task ExecuteAsync_WithValidPath_CreatesDirectory()
    {
        // Arrange
        var relativePath = "test-folder";
        var expectedPath = Path.Combine(_tempRoot, relativePath);
        var toolCall = new ToolCall
        {
            Id = "test-1",
            Name = _tool.Name,
            Arguments = $"{{\"path\":\"{relativePath}\"}}"
        };
        var context = new ToolExecutionContext(toolCall);

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Created directory", result.Content);
        Assert.True(Directory.Exists(expectedPath));
    }

    [Fact]
    public async Task ExecuteAsync_WithNestedPath_CreatesParentDirectories()
    {
        // Arrange
        var relativePath = "parent/child/grandchild";
        var expectedPath = Path.Combine(_tempRoot, "parent", "child", "grandchild");
        var toolCall = new ToolCall
        {
            Id = "test-2",
            Name = _tool.Name,
            Arguments = $"{{\"path\":\"{relativePath}\"}}"
        };
        var context = new ToolExecutionContext(toolCall);

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(Directory.Exists(expectedPath));
        Assert.True(Directory.Exists(Path.Combine(_tempRoot, "parent", "child")));
        Assert.True(Directory.Exists(Path.Combine(_tempRoot, "parent")));
    }

    [Fact]
    public async Task ExecuteAsync_WithDotPath_CreatesInRoot()
    {
        // Arrange
        var relativePath = ".";
        var toolCall = new ToolCall
        {
            Id = "test-3",
            Name = _tool.Name,
            Arguments = $"{{\"path\":\"{relativePath}\"}}"
        };
        var context = new ToolExecutionContext(toolCall);

        // Act
        var result = await _tool.ExecuteAsync(context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(Directory.Exists(_tempRoot));
    }

    [Fact]
    public async Task ExecuteAsync_PathEscapesRoot_ThrowsException()
    {
        // Arrange
        var relativePath = "../escape-attempt";
        var toolCall = new ToolCall
        {
            Id = "test-4",
            Name = _tool.Name,
            Arguments = $"{{\"path\":\"{relativePath}\"}}"
        };
        var context = new ToolExecutionContext(toolCall);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _tool.ExecuteAsync(context));
        Assert.Contains("escapes the configured filesystem root", exception.Message);
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        Assert.Equal("create_directory", _tool.Name);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(_tool.Description));
        Assert.Contains("directory", _tool.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParametersSchema_IsNotNull()
    {
        Assert.NotNull(_tool.ParametersSchema);
    }
}
