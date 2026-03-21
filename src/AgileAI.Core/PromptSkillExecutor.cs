using AgileAI.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgileAI.Core;

public class PromptSkillExecutor : ISkillExecutor
{
    private readonly IChatClient _chatClient;
    private readonly IToolRegistry? _toolRegistry;
    private readonly ILogger<PromptSkillExecutor>? _logger;

    public PromptSkillExecutor(
        IChatClient chatClient,
        IToolRegistry? toolRegistry = null,
        ILogger<PromptSkillExecutor>? logger = null)
    {
        _chatClient = chatClient;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task<AgentResult> ExecuteAsync(
        SkillManifest manifest,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var modelId = context.ModelId ?? context.Request.ModelId ?? throw new InvalidOperationException("ModelId is required");

        var preparedHistory = SkillPromptHelper.PrepareHistoryForSkill(context.Request.History, manifest).ToList();
        var originalHistoryCount = context.Request.History?.Count ?? 0;
        var insertedMessages = preparedHistory.Count - originalHistoryCount;

        var messages = preparedHistory;
        messages.Add(ChatMessage.User(context.Request.Input));

        var options = new ChatOptions();
        var toolCount = 0;
        if (_toolRegistry != null)
        {
            var tools = _toolRegistry.GetToolDefinitions();
            toolCount = tools.Count;
            if (toolCount > 0)
            {
                options = options with { Tools = tools };
            }
        }

        _logger?.LogInformation(
            "Executing local skill {SkillName}. ModelId={ModelId}, OriginalHistory={OriginalHistory}, PreparedHistory={PreparedHistory}, InsertedMessages={InsertedMessages}, ToolCount={ToolCount}",
            manifest.Name,
            modelId,
            originalHistoryCount,
            preparedHistory.Count,
            insertedMessages,
            toolCount);

        var response = await _chatClient.CompleteAsync(new ChatRequest
        {
            ModelId = modelId,
            Messages = messages,
            Options = options
        }, cancellationToken);

        _logger?.LogInformation(
            "Local skill execution completed. Skill={SkillName}, Success={Success}, Error={Error}",
            manifest.Name,
            response.IsSuccess,
            response.ErrorMessage);

        return new AgentResult
        {
            IsSuccess = response.IsSuccess,
            Output = response.Message?.TextContent ?? string.Empty,
            ErrorMessage = response.ErrorMessage,
            UpdatedHistory = messages.Concat(response.Message != null ? [response.Message] : []).ToList()
        };
    }
}
