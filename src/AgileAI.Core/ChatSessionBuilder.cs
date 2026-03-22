using AgileAI.Abstractions;
using Microsoft.Extensions.Logging;

namespace AgileAI.Core;

public class ChatSessionBuilder(IChatClient chatClient, string modelId)
{
    private IToolRegistry? _toolRegistry;
    private int _maxToolLoopIterations = 5;
    private IServiceProvider? _serviceProvider;
    private ILogger<ChatSession>? _logger;
    private IReadOnlyList<ChatMessage> _history = [];

    public ChatSessionBuilder WithToolRegistry(IToolRegistry? toolRegistry)
    {
        _toolRegistry = toolRegistry;
        return this;
    }

    public ChatSessionBuilder WithMaxToolLoopIterations(int maxToolLoopIterations)
    {
        _maxToolLoopIterations = maxToolLoopIterations;
        return this;
    }

    public ChatSessionBuilder WithServiceProvider(IServiceProvider? serviceProvider)
    {
        _serviceProvider = serviceProvider;
        return this;
    }

    public ChatSessionBuilder WithLogger(ILogger<ChatSession>? logger)
    {
        _logger = logger;
        return this;
    }

    public ChatSessionBuilder WithHistory(IEnumerable<ChatMessage> history)
    {
        _history = history.ToList();
        return this;
    }

    public ChatSession Build()
    {
        var session = new ChatSession(
            chatClient,
            modelId,
            _toolRegistry,
            _maxToolLoopIterations,
            _serviceProvider,
            _logger);

        foreach (var message in _history)
        {
            session.AddMessage(message);
        }

        return session;
    }
}
