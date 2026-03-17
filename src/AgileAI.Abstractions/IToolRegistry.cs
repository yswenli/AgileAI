namespace AgileAI.Abstractions;

public interface IToolRegistry
{
    void Register(ITool tool);
    void Register(IEnumerable<ITool> tools);
    bool TryGetTool(string name, out ITool? tool);
    IReadOnlyList<ITool> GetAllTools();
    IReadOnlyList<ToolDefinition> GetToolDefinitions();
}
