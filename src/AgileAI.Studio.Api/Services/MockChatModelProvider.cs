using System.Runtime.CompilerServices;
using AgileAI.Abstractions;

namespace AgileAI.Studio.Api.Services;

public class MockChatModelProvider(string providerName) : IChatModelProvider
{
    public string ProviderName { get; } = providerName;

    public Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var output = BuildReply(request);
        return Task.FromResult(new ChatResponse
        {
            IsSuccess = true,
            FinishReason = "stop",
            Usage = new UsageInfo
            {
                PromptTokens = Math.Max(8, output.Length / 4),
                CompletionTokens = Math.Max(10, output.Length / 5),
                TotalTokens = Math.Max(18, output.Length / 2)
            },
            Message = ChatMessage.Assistant(output)
        });
    }

    public async IAsyncEnumerable<StreamingChatUpdate> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var output = BuildReply(request);
        foreach (var chunk in Chunk(output, 18))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(18, cancellationToken);
            yield return new TextDeltaUpdate(chunk);
        }

        yield return new UsageUpdate(new UsageInfo
        {
            PromptTokens = Math.Max(8, output.Length / 4),
            CompletionTokens = Math.Max(10, output.Length / 5),
            TotalTokens = Math.Max(18, output.Length / 2)
        });
        yield return new CompletedUpdate("stop");
    }

    private static string BuildReply(ChatRequest request)
    {
        var lastUser = request.Messages.LastOrDefault(x => x.Role == ChatRole.User)?.TextContent?.Trim() ?? "";
        if (lastUser.Contains("Respond with OK", StringComparison.OrdinalIgnoreCase))
        {
            return "OK";
        }

        return $"Mock response from AgileAI Studio for: {lastUser}\n\n- This is a local demo reply\n- Streaming and persistence are active\n- Replace the seeded API key to use a real provider";
    }

    private static IEnumerable<string> Chunk(string input, int size)
    {
        for (var index = 0; index < input.Length; index += size)
        {
            yield return input.Substring(index, Math.Min(size, input.Length - index));
        }
    }
}
