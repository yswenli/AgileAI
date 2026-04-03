namespace AgileAI.Abstractions;

public interface IChatTurnMiddleware
{
    Task<ChatTurnResult> InvokeAsync(
        ChatTurnExecutionContext context,
        Func<Task<ChatTurnResult>> next,
        CancellationToken cancellationToken = default);
}
