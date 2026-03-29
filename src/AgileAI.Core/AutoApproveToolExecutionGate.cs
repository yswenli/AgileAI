using AgileAI.Abstractions;

namespace AgileAI.Core;

public sealed class AutoApproveToolExecutionGate : IToolExecutionGate
{
    public Task<ToolApprovalDecision> EvaluateAsync(ToolApprovalRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(ToolApprovalDecision.ApprovedDecision(request.Id));
}
