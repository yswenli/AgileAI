using AgileAI.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgileAI.Core;

public sealed class LoggingStreamingChatTurnMiddleware(
    ILogger<LoggingStreamingChatTurnMiddleware>? logger = null,
    LoggingMiddlewareOptions? options = null) : IStreamingChatTurnMiddleware
{
    private readonly ILogger<LoggingStreamingChatTurnMiddleware>? _logger = logger;
    private readonly LoggingMiddlewareOptions _options = options ?? new LoggingMiddlewareOptions();

    public async IAsyncEnumerable<ChatTurnStreamUpdate> InvokeAsync(
        StreamingChatTurnExecutionContext context,
        Func<IAsyncEnumerable<ChatTurnStreamUpdate>> next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_options.LogInputs)
        {
            _logger?.LogInformation(
                "Starting streaming chat turn. Kind={Kind}, ModelId={ModelId}, Input={Input}, MessageCount={MessageCount}",
                context.Kind,
                context.ModelId,
                context.Input,
                _options.IncludeMessageCounts ? context.Messages.Count : 0);
        }
        else
        {
            _logger?.LogInformation(
                "Starting streaming chat turn. Kind={Kind}, ModelId={ModelId}, InputLength={InputLength}, MessageCount={MessageCount}",
                context.Kind,
                context.ModelId,
                context.Input?.Length ?? 0,
                _options.IncludeMessageCounts ? context.Messages.Count : 0);
        }

        await foreach (var update in StreamUpdatesAsync(context, next, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return update;
        }
    }

    private async IAsyncEnumerable<ChatTurnStreamUpdate> StreamUpdatesAsync(
        StreamingChatTurnExecutionContext context,
        Func<IAsyncEnumerable<ChatTurnStreamUpdate>> next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var enumerator = next().GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            ChatTurnStreamUpdate update;
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    yield break;
                }

                update = enumerator.Current;
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Streaming chat turn failed. Kind={Kind}, ModelId={ModelId}",
                    context.Kind,
                    context.ModelId);
                throw;
            }

            if (update is ChatTurnCompleted completed)
            {
                _logger?.LogInformation(
                    "Completed streaming chat turn. Kind={Kind}, ModelId={ModelId}, Success={Success}, ToolResultCount={ToolResultCount}",
                    context.Kind,
                    context.ModelId,
                    completed.Response.IsSuccess,
                    completed.ToolResults?.Count ?? 0);
            }
            else if (update is ChatTurnPendingApproval pendingApproval)
            {
                _logger?.LogWarning(
                    "Streaming chat turn pending approval. Kind={Kind}, ModelId={ModelId}, ToolName={ToolName}",
                    context.Kind,
                    context.ModelId,
                    pendingApproval.PendingApprovalRequest.ToolName);
            }
            else if (update is ChatTurnError error)
            {
                _logger?.LogError(
                    "Streaming chat turn emitted error. Kind={Kind}, ModelId={ModelId}, Error={Error}",
                    context.Kind,
                    context.ModelId,
                    error.ErrorMessage);
            }

            yield return update;
        }
    }
}
