namespace AgileAI.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class NeedApprovalAttribute : Attribute
{
    public NeedApprovalAttribute(ToolApprovalMode approvalMode = ToolApprovalMode.PerExecution)
    {
        ApprovalMode = approvalMode;
    }

    public ToolApprovalMode ApprovalMode { get; }
}
