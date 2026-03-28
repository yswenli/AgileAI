namespace AgileAI.Studio.Api.Domain;

public class AgentToolSelection
{
    public Guid AgentDefinitionId { get; set; }
    public string ToolNamesJson { get; set; } = "[]";
}
