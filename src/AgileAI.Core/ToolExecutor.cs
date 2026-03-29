using AgileAI.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgileAI.Core;

public sealed class ToolExecutor(IToolExecutionGate executionGate, ILogger<ToolExecutor>? logger = null)
{
    public async Task<(ToolResult Result, ToolApprovalRequest? PendingApprovalRequest)> ExecuteAsync(
        ITool tool,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ToolApprovalRequest? approvalRequest = null;

        if (tool is IApprovalAwareTool approvalAwareTool && approvalAwareTool.ApprovalMode == ToolApprovalMode.PerExecution)
        {
            approvalRequest = new ToolApprovalRequest
            {
                ToolCallId = context.ToolCall.Id,
                ToolName = tool.Name,
                Arguments = context.ToolCall.Arguments,
                SessionId = context.SessionId,
                ConversationId = context.ConversationId,
                ChatHistory = context.ChatHistory
            };

            var decision = await executionGate.EvaluateAsync(approvalRequest, cancellationToken);
            if (decision.IsPending)
            {
                return (new ToolResult
                {
                    ToolCallId = context.ToolCall.Id,
                    Content = decision.Comment ?? $"Execution of tool '{tool.Name}' is waiting for approval.",
                    IsSuccess = false,
                    Status = ToolExecutionStatus.AwaitingApproval,
                    ApprovalRequestId = approvalRequest.Id
                }, approvalRequest);
            }

            if (!decision.Approved)
            {
                return (new ToolResult
                {
                    ToolCallId = context.ToolCall.Id,
                    Content = decision.Comment ?? $"Execution of tool '{tool.Name}' was denied.",
                    IsSuccess = false,
                    Status = ToolExecutionStatus.Denied,
                    ApprovalRequestId = approvalRequest.Id
                }, null);
            }
        }

        try
        {
            var result = await tool.ExecuteAsync(context, cancellationToken);
            return (approvalRequest == null ? result : result with { ApprovalRequestId = approvalRequest.Id }, null);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error executing tool '{ToolName}'", tool.Name);
            return (new ToolResult
            {
                ToolCallId = context.ToolCall.Id,
                Content = $"Error executing tool '{tool.Name}': {ex.Message}",
                IsSuccess = false,
                Status = ToolExecutionStatus.Failed,
                ApprovalRequestId = approvalRequest?.Id
            }, null);
        }
    }
}
