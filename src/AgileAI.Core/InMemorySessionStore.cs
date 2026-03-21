using System.Collections.Concurrent;
using AgileAI.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgileAI.Core;

public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, ConversationState> _states = new(StringComparer.Ordinal);
    private readonly ILogger<InMemorySessionStore>? _logger;

    public InMemorySessionStore(ILogger<InMemorySessionStore>? logger = null)
    {
        _logger = logger;
    }

    public Task<ConversationState?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _states.TryGetValue(sessionId, out var state);
        _logger?.LogDebug("SessionStore GetAsync. SessionId={SessionId}, Found={Found}", sessionId, state != null);
        return Task.FromResult(state);
    }

    public Task SaveAsync(ConversationState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (string.IsNullOrWhiteSpace(state.SessionId))
        {
            throw new ArgumentException("SessionId is required", nameof(state));
        }

        _states[state.SessionId] = state;
        _logger?.LogDebug(
            "SessionStore SaveAsync. SessionId={SessionId}, HistoryCount={HistoryCount}, ActiveSkill={ActiveSkill}, TotalSessions={TotalSessions}",
            state.SessionId,
            state.History?.Count ?? 0,
            state.ActiveSkill,
            _states.Count);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _states.TryRemove(sessionId, out _);
        _logger?.LogDebug("SessionStore DeleteAsync. SessionId={SessionId}, TotalSessions={TotalSessions}", sessionId, _states.Count);
        return Task.CompletedTask;
    }
}
