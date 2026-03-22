using AgileAI.Abstractions;
using AgileAI.Core;

namespace AgileAI.Extensions.FileSystem;

public class FileSystemToolRegistryFactory(
    ListDirectoryTool listDirectoryTool,
    ReadFileTool readFileTool,
    WriteFileTool writeFileTool)
{
    public IToolRegistry CreateDefaultRegistry()
    {
        var registry = new InMemoryToolRegistry();
        registry.Register([listDirectoryTool, readFileTool, writeFileTool]);
        return registry;
    }
}
