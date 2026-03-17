using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AgileAI.Abstractions;

namespace AgileAI.Providers.OpenAI;

public class OpenAIChatModelProvider : IChatModelProvider
{
    public string ProviderName => "openai";
    
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<OpenAIChatModelProvider>? _logger;

    public OpenAIChatModelProvider(
        HttpClient httpClient,
        OpenAIOptions options,
        ILogger<OpenAIChatModelProvider>? logger = null)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        _httpClient.BaseAddress = new Uri(options.BaseUrl ?? "https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
    }

    public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString();
        _logger?.LogInformation("Starting request {RequestId} to model {ModelId} via {ProviderName}",
            requestId, request.ModelId, ProviderName);

        try
        {
            var openAiRequest = MapToOpenAIRequest(request);
            var json = JsonSerializer.Serialize(openAiRequest, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var openAiResponse = JsonSerializer.Deserialize<OpenAIChatCompletionResponse>(responseJson, _jsonOptions);
            
            _logger?.LogInformation("Request {RequestId} completed successfully", requestId);
            return MapFromOpenAIResponse(openAiResponse);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Request {RequestId} failed", requestId);
            return new ChatResponse
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async IAsyncEnumerable<StreamingChatUpdate> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString();
        _logger?.LogInformation("Starting streaming request {RequestId} to model {ModelId} via {ProviderName}",
            requestId, request.ModelId, ProviderName);

        HttpResponseMessage? response = null;
        Exception? initialException = null;
        
        try
        {
            var openAiRequest = MapToOpenAIRequest(request);
            openAiRequest.Stream = true;
            var json = JsonSerializer.Serialize(openAiRequest, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = content
            };
            
            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Streaming request {RequestId} failed during initialization", requestId);
            initialException = ex;
        }

        if (initialException != null)
        {
            yield return new ErrorUpdate(initialException.Message);
            yield break;
        }

        if (response == null)
        {
            yield break;
        }

        var toolCallAccumulators = new Dictionary<int, ToolCallAccumulator>();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
                
            if (line.StartsWith("data: "))
                line = line["data: ".Length..];
                
            if (line == "[DONE]")
                break;
                
            OpenAIChatCompletionStreamResponse? streamingResponse = null;
            try
            {
                streamingResponse = JsonSerializer.Deserialize<OpenAIChatCompletionStreamResponse>(line, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Failed to deserialize streaming line");
                continue;
            }

            if (streamingResponse?.Choices != null && streamingResponse.Choices.Count > 0)
            {
                var choice = streamingResponse.Choices[0];
                var delta = choice.Delta;
                
                if (!string.IsNullOrEmpty(delta?.Content))
                {
                    yield return new TextDeltaUpdate(delta.Content);
                }
                
                if (delta?.ToolCalls != null && delta.ToolCalls.Count > 0)
                {
                    foreach (var toolCall in delta.ToolCalls)
                    {
                        if (!toolCall.Index.HasValue)
                            continue;

                        var index = toolCall.Index.Value;
                        
                        if (!toolCallAccumulators.TryGetValue(index, out var accumulator))
                        {
                            accumulator = new ToolCallAccumulator();
                            toolCallAccumulators[index] = accumulator;
                        }

                        if (!string.IsNullOrEmpty(toolCall.Id))
                        {
                            accumulator.Id = toolCall.Id;
                        }

                        if (!string.IsNullOrEmpty(toolCall.Function?.Name))
                        {
                            accumulator.Name = toolCall.Function.Name;
                        }

                        if (!string.IsNullOrEmpty(toolCall.Function?.Arguments))
                        {
                            accumulator.Arguments += toolCall.Function.Arguments;
                        }

                        yield return new ToolCallDeltaUpdate(
                            accumulator.Id ?? string.Empty,
                            toolCall.Function?.Name,
                            toolCall.Function?.Arguments
                        );
                    }
                }
                
                if (!string.IsNullOrEmpty(choice.FinishReason))
                {
                    yield return new CompletedUpdate(choice.FinishReason);
                }
            }
            
            if (streamingResponse?.Usage != null)
            {
                yield return new UsageUpdate(new UsageInfo
                {
                    PromptTokens = streamingResponse.Usage.PromptTokens,
                    CompletionTokens = streamingResponse.Usage.CompletionTokens,
                    TotalTokens = streamingResponse.Usage.TotalTokens
                });
            }
        }

        _logger?.LogInformation("Streaming request {RequestId} completed", requestId);
    }

    private OpenAIChatCompletionRequest MapToOpenAIRequest(ChatRequest request)
    {
        var messages = request.Messages.Select(MapToOpenAIMessage).ToList();
        
        var openAiRequest = new OpenAIChatCompletionRequest
        {
            Model = request.ModelId,
            Messages = messages,
            Temperature = request.Options?.Temperature,
            TopP = request.Options?.TopP,
            MaxTokens = request.Options?.MaxTokens,
            Stop = request.Options?.StopSequences
        };

        if (request.Options?.Tools != null && request.Options.Tools.Count > 0)
        {
            openAiRequest.Tools = request.Options.Tools.Select(t => new OpenAIToolDefinition
            {
                Type = "function",
                Function = new OpenAIFunctionDefinition
                {
                    Name = t.Name,
                    Description = t.Description,
                    Parameters = t.ParametersSchema
                }
            }).ToList();
        }

        return openAiRequest;
    }

    private OpenAIMessage MapToOpenAIMessage(ChatMessage message)
    {
        var role = message.Role switch
        {
            ChatRole.System => "system",
            ChatRole.User => "user",
            ChatRole.Assistant => "assistant",
            ChatRole.Tool => "tool",
            _ => "user"
        };

        var openAiMessage = new OpenAIMessage
        {
            Role = role,
            Content = message.TextContent,
            ToolCallId = message.ToolCallId
        };

        if (message.ToolCalls != null && message.ToolCalls.Count > 0)
        {
            openAiMessage.ToolCalls = message.ToolCalls.Select(tc => new OpenAIToolCall
            {
                Id = tc.Id,
                Type = "function",
                Function = new OpenAIFunctionCall
                {
                    Name = tc.Name,
                    Arguments = tc.Arguments
                }
            }).ToList();
        }

        return openAiMessage;
    }

    private ChatResponse MapFromOpenAIResponse(OpenAIChatCompletionResponse? response)
    {
        if (response == null || response.Choices == null || response.Choices.Count == 0)
        {
            return new ChatResponse
            {
                IsSuccess = false,
                ErrorMessage = "Invalid response from OpenAI"
            };
        }

        var choice = response.Choices[0];
        var message = choice.Message;

        IReadOnlyList<ToolCall>? toolCalls = null;
        if (message?.ToolCalls != null && message.ToolCalls.Count > 0)
        {
            toolCalls = message.ToolCalls.Select(tc => new ToolCall
            {
                Id = tc.Id,
                Name = tc.Function?.Name ?? string.Empty,
                Arguments = tc.Function?.Arguments ?? string.Empty
            }).ToList();
        }

        var chatMessage = new ChatMessage
        {
            Role = ChatRole.Assistant,
            TextContent = message?.Content,
            ToolCalls = toolCalls
        };

        var usage = response.Usage != null ? new UsageInfo
        {
            PromptTokens = response.Usage.PromptTokens,
            CompletionTokens = response.Usage.CompletionTokens,
            TotalTokens = response.Usage.TotalTokens
        } : null;

        return new ChatResponse
        {
            IsSuccess = true,
            Message = chatMessage,
            FinishReason = choice.FinishReason,
            Usage = usage
        };
    }

    private class ToolCallAccumulator
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string Arguments { get; set; } = string.Empty;
    }
}

// OpenAI-specific model classes
internal class OpenAIChatCompletionRequest
{
    public string Model { get; set; } = string.Empty;
    public List<OpenAIMessage> Messages { get; set; } = [];
    public double? Temperature { get; set; }
    public double? TopP { get; set; }
    public int? MaxTokens { get; set; }
    public IReadOnlyList<string>? Stop { get; set; }
    public List<OpenAIToolDefinition>? Tools { get; set; }
    public bool? Stream { get; set; }
}

internal class OpenAIMessage
{
    public string Role { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? ToolCallId { get; set; }
    public List<OpenAIToolCall>? ToolCalls { get; set; }
}

internal class OpenAIToolDefinition
{
    public string Type { get; set; } = string.Empty;
    public OpenAIFunctionDefinition Function { get; set; } = null!;
}

internal class OpenAIFunctionDefinition
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public object? Parameters { get; set; }
}

internal class OpenAIToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public OpenAIFunctionCall Function { get; set; } = null!;
    public int? Index { get; set; }
}

internal class OpenAIFunctionCall
{
    public string? Name { get; set; }
    public string? Arguments { get; set; }
}

internal class OpenAIChatCompletionResponse
{
    public List<OpenAIChoice>? Choices { get; set; }
    public OpenAIUsage? Usage { get; set; }
}

internal class OpenAIChoice
{
    public OpenAIMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}

internal class OpenAIUsage
{
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
}

internal class OpenAIChatCompletionStreamResponse
{
    public List<OpenAIStreamChoice>? Choices { get; set; }
    public OpenAIUsage? Usage { get; set; }
}

internal class OpenAIStreamChoice
{
    public OpenAIDelta? Delta { get; set; }
    public string? FinishReason { get; set; }
}

internal class OpenAIDelta
{
    public string? Content { get; set; }
    public List<OpenAIToolCall>? ToolCalls { get; set; }
}
