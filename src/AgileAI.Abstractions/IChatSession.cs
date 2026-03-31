namespace AgileAI.Abstractions;

public interface IChatSession
{
    IReadOnlyList<ChatMessage> History { get; }
    void AddMessage(ChatMessage message);
    void ClearHistory();
    Task<ChatResponse> SendAsync(string message, ChatOptions? options = null, CancellationToken cancellationToken = default);
    Task<ChatTurnResult> SendTurnAsync(string message, ChatOptions? options = null, CancellationToken cancellationToken = default);
    Task<ChatTurnResult> ContinueAsync(ChatOptions? options = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatTurnStreamUpdate> StreamTurnAsync(string message, ChatOptions? options = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatTurnStreamUpdate> ContinueStreamAsync(ChatOptions? options = null, CancellationToken cancellationToken = default);
}
