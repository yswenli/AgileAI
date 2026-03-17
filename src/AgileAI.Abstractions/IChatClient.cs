namespace AgileAI.Abstractions;

public interface IChatClient
{
    Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<StreamingChatUpdate> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
