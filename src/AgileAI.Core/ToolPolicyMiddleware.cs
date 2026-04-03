using AgileAI.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgileAI.Core;

public sealed class ToolPolicyMiddleware : IToolExecutionMiddleware
{
    private readonly HashSet<string>? _allowedToolNames;
    private readonly HashSet<string> _deniedToolNames;
    private readonly string? _denialMessage;
    private readonly ILogger<ToolPolicyMiddleware>? _logger;

    public ToolPolicyMiddleware(
        ToolPolicyOptions? options = null,
        ILogger<ToolPolicyMiddleware>? logger = null)
    {
        _logger = logger;
        _denialMessage = options?.DenialMessage;
        _allowedToolNames = options?.AllowedToolNames == null
            ? null
            : new HashSet<string>(options.AllowedToolNames, StringComparer.OrdinalIgnoreCase);
        _deniedToolNames = options?.DeniedToolNames == null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(options.DeniedToolNames, StringComparer.OrdinalIgnoreCase);
    }

    public Task<ToolExecutionOutcome> InvokeAsync(
        ToolExecutionMiddlewareContext context,
        Func<Task<ToolExecutionOutcome>> next,
        CancellationToken cancellationToken = default)
    {
        if (IsDenied(context.Tool.Name))
        {
            _logger?.LogWarning(
                "Tool execution denied by policy. Tool={ToolName}, ToolCallId={ToolCallId}",
                context.Tool.Name,
                context.ExecutionContext.ToolCall.Id);

            return Task.FromResult(new ToolExecutionOutcome
            {
                Result = new ToolResult
                {
                    ToolCallId = context.ExecutionContext.ToolCall.Id,
                    Content = _denialMessage ?? $"Execution of tool '{context.Tool.Name}' was denied by policy.",
                    IsSuccess = false,
                    Status = ToolExecutionStatus.Denied
                }
            });
        }

        return next();
    }

    private bool IsDenied(string toolName)
    {
        if (_deniedToolNames.Contains(toolName))
        {
            return true;
        }

        if (_allowedToolNames is { Count: > 0 } && !_allowedToolNames.Contains(toolName))
        {
            return true;
        }

        return false;
    }
}
