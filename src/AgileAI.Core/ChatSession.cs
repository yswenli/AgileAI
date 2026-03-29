using Microsoft.Extensions.Logging;
using AgileAI.Abstractions;

namespace AgileAI.Core;

public class ChatSession : IChatSession
{
    private readonly IChatClient _chatClient;
    private readonly string _modelId;
    private readonly List<ChatMessage> _history = new();
    private readonly IToolRegistry? _toolRegistry;
    private readonly int _maxToolLoopIterations;
    private readonly IToolExecutionGate _toolExecutionGate;
    private readonly string? _sessionId;
    private readonly string? _conversationId;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ILogger<ChatSession>? _logger;
    private readonly ToolExecutor _toolExecutor;

    public IReadOnlyList<ChatMessage> History => _history.AsReadOnly();

    public ChatSession(
        IChatClient chatClient,
        string modelId,
        IToolRegistry? toolRegistry = null,
        int maxToolLoopIterations = 5,
        IToolExecutionGate? toolExecutionGate = null,
        string? sessionId = null,
        string? conversationId = null,
        IServiceProvider? serviceProvider = null,
        ILogger<ChatSession>? logger = null)
    {
        _chatClient = chatClient;
        _modelId = modelId;
        _toolRegistry = toolRegistry;
        _maxToolLoopIterations = maxToolLoopIterations;
        _toolExecutionGate = toolExecutionGate ?? new AutoApproveToolExecutionGate();
        _sessionId = sessionId;
        _conversationId = conversationId;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _toolExecutor = new ToolExecutor(_toolExecutionGate, serviceProvider?.GetService(typeof(ILogger<ToolExecutor>)) as ILogger<ToolExecutor>);
    }

    public void AddMessage(ChatMessage message)
    {
        _history.Add(message);
    }

    public void ClearHistory()
    {
        _history.Clear();
    }

    public async Task<ChatResponse> SendAsync(string message, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var result = await SendTurnAsync(message, options, cancellationToken);
        return result.Response;
    }

    public Task<ChatTurnResult> ContinueAsync(ChatOptions? options = null, CancellationToken cancellationToken = default)
        => ExecuteTurnLoopAsync(options, cancellationToken);

    public async Task<ChatTurnResult> SendTurnAsync(string message, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        AddMessage(ChatMessage.User(message));
        return await ExecuteTurnLoopAsync(options, cancellationToken);
    }

    private async Task<ChatTurnResult> ExecuteTurnLoopAsync(ChatOptions? options, CancellationToken cancellationToken)
    {
        var finalOptions = options ?? new ChatOptions();
        if (_toolRegistry != null)
        {
            var toolDefs = _toolRegistry.GetToolDefinitions();
            if (toolDefs.Count > 0)
            {
                finalOptions = finalOptions with
                {
                    Tools = toolDefs
                };
            }
        }

        ChatResponse? lastResponse = null;
        List<ToolResult>? lastToolResults = null;
        var iteration = 0;

        while (iteration < _maxToolLoopIterations)
        {
            _logger?.LogInformation("ChatSession iteration {Iteration}/{MaxIterations}", iteration + 1, _maxToolLoopIterations);
            
            var request = new ChatRequest
            {
                ModelId = _modelId,
                Messages = _history.AsReadOnly(),
                Options = finalOptions
            };

            lastResponse = await _chatClient.CompleteAsync(request, cancellationToken);

            if (!lastResponse.IsSuccess)
            {
                _logger?.LogError("ChatSession request failed: {ErrorMessage}", lastResponse.ErrorMessage);
                return new ChatTurnResult { Response = lastResponse };
            }

            if (lastResponse.Message != null)
            {
                _history.Add(lastResponse.Message);
            }

            var toolCalls = lastResponse.Message?.ToolCalls;
            if (toolCalls == null || toolCalls.Count == 0 || _toolRegistry == null)
            {
                _logger?.LogInformation("ChatSession completed without tool calls");
                break;
            }

            _logger?.LogInformation("ChatSession executing {ToolCount} tool calls", toolCalls.Count);
            var toolResults = new List<ToolResult>();
            foreach (var toolCall in toolCalls)
            {
                _logger?.LogInformation("Executing tool: {ToolName}", toolCall.Name);
                if (_toolRegistry.TryGetTool(toolCall.Name, out var tool) && tool != null)
                {
                    var context = new ToolExecutionContext
                    {
                        ToolCall = toolCall,
                        ChatHistory = _history.AsReadOnly(),
                        ServiceProvider = _serviceProvider,
                        SessionId = _sessionId,
                        ConversationId = _conversationId
                    };
                    var execution = await _toolExecutor.ExecuteAsync(tool, context, cancellationToken);
                    toolResults.Add(execution.Result);

                    if (execution.PendingApprovalRequest != null)
                    {
                        lastToolResults = toolResults;
                        return new ChatTurnResult
                        {
                            Response = lastResponse,
                            ToolResults = lastToolResults,
                            PendingApprovalRequest = execution.PendingApprovalRequest
                        };
                    }
                }
                else
                {
                    _logger?.LogWarning("Tool '{ToolName}' not found", toolCall.Name);
                    toolResults.Add(new ToolResult
                    {
                        ToolCallId = toolCall.Id,
                        Content = $"Tool '{toolCall.Name}' not found",
                        IsSuccess = false,
                        Status = ToolExecutionStatus.Failed
                    });
                }
            }

            lastToolResults = toolResults;

            foreach (var result in toolResults)
            {
                _history.Add(new ChatMessage
                {
                    Role = ChatRole.Tool,
                    ToolCallId = result.ToolCallId,
                    TextContent = result.Content
                });
            }

            iteration++;
        }

        return new ChatTurnResult
        {
            Response = lastResponse ?? new ChatResponse
            {
                IsSuccess = false,
                ErrorMessage = "No response received"
            },
            ToolResults = lastToolResults
        };
    }
}
