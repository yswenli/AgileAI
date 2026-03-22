using AgileAI.Abstractions;
using AgileAI.Core;

namespace AgileAI.Studio.Api.Tools;

public class StudioToolRegistryFactory(
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
