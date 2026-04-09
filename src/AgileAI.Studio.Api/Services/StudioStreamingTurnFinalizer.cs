using AgileAI.Studio.Api.Domain;

namespace AgileAI.Studio.Api.Services;

public sealed class StudioStreamingTurnFinalizer(ConversationService conversationService)
{
    public async Task FinalizePendingApprovalAsync(
        Conversation conversation,
        ConversationMessage assistant,
        HttpResponse response,
        string waitingContent,
        object approvalPayload,
        string? appliedSkillName,
        IReadOnlyList<string>? appliedToolNames,
        Func<CancellationToken, Task> persistAsync,
        CancellationToken cancellationToken)
    {
        await conversationService.UpdateMessageAsync(
            assistant,
            waitingContent,
            false,
            null,
            null,
            null,
            appliedSkillName,
            appliedToolNames,
            cancellationToken);

        await persistAsync(cancellationToken);

        await StudioSseWriter.WriteAsync(response, "approval-required", approvalPayload, cancellationToken);
        await StudioSseWriter.WriteAsync(response, "final-message", new
        {
            content = waitingContent,
            finishReason = (string?)null,
            inputTokens = (int?)null,
            outputTokens = (int?)null,
            appliedSkillName,
            appliedToolNames
        }, cancellationToken);
        await StudioSseWriter.WriteAsync(response, "completed", new { finishReason = "approval_required" }, cancellationToken);
        await conversationService.TouchConversationAsync(conversation, cancellationToken);
    }

    public async Task FinalizeCompletedAsync(
        Conversation conversation,
        ConversationMessage assistant,
        HttpResponse response,
        string content,
        string? finishReason,
        int? inputTokens,
        int? outputTokens,
        string? appliedSkillName,
        IReadOnlyList<string>? appliedToolNames,
        Func<CancellationToken, Task> persistAsync,
        CancellationToken cancellationToken)
    {
        await conversationService.UpdateMessageAsync(
            assistant,
            content,
            false,
            finishReason,
            inputTokens,
            outputTokens,
            appliedSkillName,
            appliedToolNames,
            cancellationToken);

        await persistAsync(cancellationToken);

        await StudioSseWriter.WriteAsync(response, "final-message", new
        {
            content,
            finishReason,
            inputTokens,
            outputTokens,
            appliedSkillName,
            appliedToolNames
        }, cancellationToken);
        await StudioSseWriter.WriteAsync(response, "completed", new { finishReason }, cancellationToken);
        await conversationService.TouchConversationAsync(conversation, cancellationToken);
    }
}
