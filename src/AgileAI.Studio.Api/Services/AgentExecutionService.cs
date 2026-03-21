using System.Text;
using AgileAI.Abstractions;
using AgileAI.Studio.Api.Contracts;
using AgileAI.Studio.Api.Domain;

namespace AgileAI.Studio.Api.Services;

public class AgentExecutionService(
    ConversationService conversationService,
    ModelCatalogService modelCatalogService,
    ProviderClientFactory providerClientFactory)
{
    public async Task<ChatResultDto> SendMessageAsync(Guid conversationId, string content, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetConversationEntityAsync(conversationId, cancellationToken);
        var agent = conversation.AgentDefinition ?? throw new InvalidOperationException("Conversation agent is missing.");
        var runtime = await modelCatalogService.GetRuntimeOptionsAsync(agent.StudioModelId, cancellationToken);
        var chatClient = providerClientFactory.CreateClient(runtime);

        var userMessage = await conversationService.AddMessageAsync(conversation.Id, MessageRole.User, content.Trim(), false, null, null, null, cancellationToken);

        var request = BuildChatRequest(conversation, agent, runtime.RuntimeModelId, content.Trim());
        var response = await chatClient.CompleteAsync(request, cancellationToken);
        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(response.ErrorMessage ?? "Model request failed.");
        }

        var assistantText = response.Message?.TextContent ?? string.Empty;
        var assistant = await conversationService.AddMessageAsync(
            conversation.Id,
            MessageRole.Assistant,
            assistantText,
            false,
            response.FinishReason,
            response.Usage?.PromptTokens,
            response.Usage?.CompletionTokens,
            cancellationToken);

        await conversationService.TouchConversationAsync(conversation, cancellationToken);

        return new ChatResultDto(
            ConversationService.MapConversation(conversation),
            ConversationService.MapMessage(userMessage),
            ConversationService.MapMessage(assistant));
    }

    public async Task StreamMessageAsync(Guid conversationId, string content, HttpResponse response, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetConversationEntityAsync(conversationId, cancellationToken);
        var agent = conversation.AgentDefinition ?? throw new InvalidOperationException("Conversation agent is missing.");
        var runtime = await modelCatalogService.GetRuntimeOptionsAsync(agent.StudioModelId, cancellationToken);
        var chatClient = providerClientFactory.CreateClient(runtime);

        var trimmedContent = content.Trim();

        var userMessage = await conversationService.AddMessageAsync(conversation.Id, MessageRole.User, trimmedContent, false, null, null, null, cancellationToken);
        var assistant = await conversationService.AddMessageAsync(conversation.Id, MessageRole.Assistant, string.Empty, true, null, null, null, cancellationToken);

        response.Headers.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";

        var builder = new StringBuilder();
        string? finishReason = null;
        int? inputTokens = null;
        int? outputTokens = null;

        await WriteSseAsync(response, "message-created", new
        {
            userMessage = ConversationService.MapMessage(userMessage),
            assistantMessage = ConversationService.MapMessage(assistant),
            conversation = ConversationService.MapConversation(conversation)
        }, cancellationToken);

        var request = BuildChatRequest(conversation, agent, runtime.RuntimeModelId, trimmedContent);

        try
        {
            await foreach (var update in chatClient.StreamAsync(request, cancellationToken))
            {
                switch (update)
                {
                    case TextDeltaUpdate text:
                        builder.Append(text.Delta);
                        await WriteSseAsync(response, "text-delta", new { delta = text.Delta }, cancellationToken);
                        break;
                    case UsageUpdate usage:
                        inputTokens = usage.Usage.PromptTokens;
                        outputTokens = usage.Usage.CompletionTokens;
                        await WriteSseAsync(response, "usage", new { inputTokens, outputTokens }, cancellationToken);
                        break;
                    case CompletedUpdate completed:
                        finishReason = completed.FinishReason;
                        await WriteSseAsync(response, "completed", new { finishReason }, cancellationToken);
                        break;
                    case ErrorUpdate error:
                        await conversationService.UpdateMessageAsync(assistant, builder.ToString(), false, finishReason, inputTokens, outputTokens, cancellationToken);
                        await WriteSseAsync(response, "error", new { message = error.ErrorMessage }, cancellationToken);
                        return;
                }
            }

            var finalContent = builder.ToString();
            await conversationService.UpdateMessageAsync(assistant, finalContent, false, finishReason, inputTokens, outputTokens, cancellationToken);
            await WriteSseAsync(response, "final-message", new
            {
                content = finalContent,
                finishReason,
                inputTokens,
                outputTokens
            }, cancellationToken);
            await conversationService.TouchConversationAsync(conversation, cancellationToken);
        }
        catch (Exception ex)
        {
            await conversationService.UpdateMessageAsync(assistant, builder.ToString(), false, finishReason, inputTokens, outputTokens, cancellationToken);
            await WriteSseAsync(response, "error", new { message = ex.Message }, cancellationToken);
        }
    }

    private static ChatRequest BuildChatRequest(Conversation conversation, AgentDefinition agent, string runtimeModelId, string latestInput)
    {
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(agent.SystemPrompt))
        {
            messages.Add(ChatMessage.System(agent.SystemPrompt));
        }

        foreach (var message in conversation.Messages.OrderBy(x => x.CreatedAtUtc))
        {
            if (message.Role == MessageRole.System)
            {
                messages.Add(ChatMessage.System(message.Content));
            }
            else if (message.Role == MessageRole.User)
            {
                messages.Add(ChatMessage.User(message.Content));
            }
            else if (message.Role == MessageRole.Assistant)
            {
                messages.Add(ChatMessage.Assistant(message.Content));
            }
        }

        messages.Add(ChatMessage.User(latestInput));

        return new ChatRequest
        {
            ModelId = runtimeModelId,
            Messages = messages,
            Options = new ChatOptions
            {
                Temperature = agent.Temperature,
                MaxTokens = agent.MaxTokens
            }
        };
    }

    private static async Task WriteSseAsync(HttpResponse response, string eventName, object payload, CancellationToken cancellationToken)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        await response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
