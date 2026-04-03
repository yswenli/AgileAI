using AgileAI.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgileAI.Core;

public sealed class LoggingChatTurnMiddleware(
    ILogger<LoggingChatTurnMiddleware>? logger = null,
    LoggingMiddlewareOptions? options = null) : IChatTurnMiddleware
{
    private readonly ILogger<LoggingChatTurnMiddleware>? _logger = logger;
    private readonly LoggingMiddlewareOptions _options = options ?? new LoggingMiddlewareOptions();

    public async Task<ChatTurnResult> InvokeAsync(
        ChatTurnExecutionContext context,
        Func<Task<ChatTurnResult>> next,
        CancellationToken cancellationToken = default)
    {
        LogStart(context);

        try
        {
            var result = await next();
            LogCompletion(context, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Chat turn failed. Kind={Kind}, ModelId={ModelId}",
                context.Kind,
                context.ModelId);
            throw;
        }
    }

    private void LogStart(ChatTurnExecutionContext context)
    {
        if (_options.LogInputs)
        {
            _logger?.LogInformation(
                "Starting chat turn. Kind={Kind}, ModelId={ModelId}, Input={Input}, MessageCount={MessageCount}",
                context.Kind,
                context.ModelId,
                context.Input,
                _options.IncludeMessageCounts ? context.Messages.Count : 0);
            return;
        }

        _logger?.LogInformation(
            "Starting chat turn. Kind={Kind}, ModelId={ModelId}, InputLength={InputLength}, MessageCount={MessageCount}",
            context.Kind,
            context.ModelId,
            context.Input?.Length ?? 0,
            _options.IncludeMessageCounts ? context.Messages.Count : 0);
    }

    private void LogCompletion(ChatTurnExecutionContext context, ChatTurnResult result)
    {
        _logger?.LogInformation(
            "Completed chat turn. Kind={Kind}, ModelId={ModelId}, Success={Success}, PendingApproval={PendingApproval}, ToolResultCount={ToolResultCount}",
            context.Kind,
            context.ModelId,
            result.Response.IsSuccess,
            result.PendingApprovalRequest != null,
            result.ToolResults?.Count ?? 0);
    }
}
