namespace AgileAI.Abstractions;

public static class ToolApprovalMetadataResolver
{
    public static ToolApprovalMode ResolveApprovalMode(ITool tool)
    {
        if (tool is IApprovalAwareTool approvalAwareTool)
        {
            return approvalAwareTool.ApprovalMode;
        }

        var attribute = Attribute.GetCustomAttribute(tool.GetType(), typeof(NeedApprovalAttribute), inherit: true) as NeedApprovalAttribute;
        return attribute?.ApprovalMode ?? ToolApprovalMode.None;
    }
}
