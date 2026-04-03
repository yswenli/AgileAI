using AgileAI.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgileAI.Core;

public sealed class LoggingToolExecutionMiddleware(
    ILogger<LoggingToolExecutionMiddleware>? logger = null,
    LoggingMiddlewareOptions? options = null) : IToolExecutionMiddleware
{
    private readonly ILogger<LoggingToolExecutionMiddleware>? _logger = logger;
    private readonly LoggingMiddlewareOptions _options = options ?? new LoggingMiddlewareOptions();

    public async Task<ToolExecutionOutcome> InvokeAsync(
        ToolExecutionMiddlewareContext context,
        Func<Task<ToolExecutionOutcome>> next,
        CancellationToken cancellationToken = default)
    {
        if (_options.LogToolArguments)
        {
            _logger?.LogInformation(
                "Starting tool execution. Tool={ToolName}, ToolCallId={ToolCallId}, Arguments={Arguments}",
                context.Tool.Name,
                context.ExecutionContext.ToolCall.Id,
                context.ExecutionContext.ToolCall.Arguments);
        }
        else
        {
            _logger?.LogInformation(
                "Starting tool execution. Tool={ToolName}, ToolCallId={ToolCallId}, ArgumentLength={ArgumentLength}",
                context.Tool.Name,
                context.ExecutionContext.ToolCall.Id,
                context.ExecutionContext.ToolCall.Arguments?.Length ?? 0);
        }

        try
        {
            var outcome = await next();
            if (outcome.Result.Status == ToolExecutionStatus.Denied)
            {
                _logger?.LogWarning(
                    "Tool execution denied. Tool={ToolName}, ToolCallId={ToolCallId}, PendingApproval={PendingApproval}",
                    context.Tool.Name,
                    context.ExecutionContext.ToolCall.Id,
                    outcome.PendingApprovalRequest != null);
            }
            else
            {
                _logger?.LogInformation(
                    "Completed tool execution. Tool={ToolName}, ToolCallId={ToolCallId}, Success={Success}, Status={Status}, ResultLength={ResultLength}",
                    context.Tool.Name,
                    context.ExecutionContext.ToolCall.Id,
                    outcome.Result.IsSuccess,
                    outcome.Result.Status,
                    _options.LogToolResults ? outcome.Result.Content.Length : 0);
            }

            if (_options.LogToolResults)
            {
                _logger?.LogDebug(
                    "Tool execution result content. Tool={ToolName}, ToolCallId={ToolCallId}, Content={Content}",
                    context.Tool.Name,
                    context.ExecutionContext.ToolCall.Id,
                    outcome.Result.Content);
            }

            return outcome;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Tool execution failed. Tool={ToolName}, ToolCallId={ToolCallId}",
                context.Tool.Name,
                context.ExecutionContext.ToolCall.Id);
            throw;
        }
    }
}
