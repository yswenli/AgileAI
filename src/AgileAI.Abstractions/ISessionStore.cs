namespace AgileAI.Abstractions;

public interface ISessionStore
{
    Task<ConversationState?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
    Task SaveAsync(ConversationState state, CancellationToken cancellationToken = default);
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
}
