namespace AgileAI.Abstractions;

public interface IStreamingChatTurnMiddleware
{
    IAsyncEnumerable<ChatTurnStreamUpdate> InvokeAsync(
        StreamingChatTurnExecutionContext context,
        Func<IAsyncEnumerable<ChatTurnStreamUpdate>> next,
        CancellationToken cancellationToken = default);
}
