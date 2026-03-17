namespace AgileAI.Abstractions;

public interface IChatModelProvider
{
    string ProviderName { get; }
    Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<StreamingChatUpdate> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
