using AgileAI.Abstractions;
using AgileAI.Core;
using AgileAI.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgileAI.Tests;

public class MiddlewareTests
{
    [Fact]
    public async Task AgentExecutionMiddleware_ShouldWrapRuntimeExecution()
    {
        var chatClient = new Mock<IChatClient>();
        chatClient
            .Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = ChatMessage.Assistant("base")
            });

        var skillRegistry = new Mock<ISkillRegistry>();
        var middleware = new RecordingAgentExecutionMiddleware();
        var runtime = new DefaultAgentRuntime(
            chatClient.Object,
            skillRegistry.Object,
            executionMiddlewares: [middleware]);

        var result = await runtime.ExecuteAsync(new AgentRequest
        {
            Input = "hello",
            ModelId = "test-model"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("base|agent-after", result.Output);
        Assert.Equal(["before:hello", "after:base"], middleware.Events);
    }

    [Fact]
    public async Task ChatTurnMiddleware_ShouldWrapSessionTurnAndModifyResult()
    {
        var chatClient = new Mock<IChatClient>();
        chatClient
            .Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = ChatMessage.Assistant("done")
            });

        var middleware = new RecordingChatTurnMiddleware();
        var session = new ChatSession(
            chatClient.Object,
            "test-model",
            chatTurnMiddlewares: [middleware]);

        var result = await session.SendTurnAsync("hello");

        Assert.True(result.Response.IsSuccess);
        Assert.Equal("done|turn-after", result.Response.Message?.TextContent);
        Assert.Equal(ChatTurnExecutionKind.SessionTurn, middleware.SeenKinds.Single());
        Assert.Equal(["before:hello", "after:done"], middleware.Events);
    }

    [Fact]
    public async Task ChatTurnMiddleware_ShouldShortCircuitPromptSkillExecution()
    {
        var chatClient = new Mock<IChatClient>(MockBehavior.Strict);
        var middleware = new ShortCircuitChatTurnMiddleware("short-circuit");
        var executor = new PromptSkillExecutor(chatClient.Object, chatTurnMiddlewares: [middleware]);

        var result = await executor.ExecuteAsync(new SkillManifest
        {
            Name = "weather",
            InstructionBody = "Always answer briefly."
        }, new SkillExecutionContext
        {
            Request = new AgentRequest
            {
                Input = "hello",
                ModelId = "test-model"
            },
            ModelId = "test-model"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("short-circuit", result.Output);
        Assert.Equal([ChatTurnExecutionKind.PromptSkill], middleware.SeenKinds);
    }

    [Fact]
    public async Task ToolExecutionMiddleware_ShouldWrapToolExecution()
    {
        var chatClient = new Mock<IChatClient>();
        var toolCall = new ToolCall { Id = "call-1", Name = "wrapped-tool", Arguments = "{}" };
        chatClient.SetupSequence(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = new ChatMessage
                {
                    Role = ChatRole.Assistant,
                    ToolCalls = [toolCall]
                }
            })
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = ChatMessage.Assistant("final")
            });

        var registry = new InMemoryToolRegistry();
        registry.Register(new DelegateTool("wrapped-tool", _ => Task.FromResult(new ToolResult
        {
            ToolCallId = toolCall.Id,
            Content = "tool-output",
            IsSuccess = true
        })));

        var middleware = new RecordingToolExecutionMiddleware();
        var session = new ChatSession(
            chatClient.Object,
            "test-model",
            registry,
            toolExecutionMiddlewares: [middleware]);

        await session.SendAsync("run tool");

        Assert.Equal(["before:wrapped-tool", "after:tool-output"], middleware.Events);
        Assert.Contains(session.History, x => x.Role == ChatRole.Tool && x.TextContent == "tool-output|tool-after");
    }

    [Fact]
    public void AddAgileAI_ShouldResolveRegisteredMiddlewares()
    {
        var services = new ServiceCollection();
        services.AddAgileAI();
        services.AddAgentExecutionMiddleware<RecordingAgentExecutionMiddleware>();
        services.AddChatTurnMiddleware<RecordingChatTurnMiddleware>();
        services.AddStreamingChatTurnMiddleware<PassthroughStreamingMiddleware>();
        services.AddToolExecutionMiddleware<RecordingToolExecutionMiddleware>();

        var provider = services.BuildServiceProvider();

        Assert.Single(provider.GetServices<IAgentExecutionMiddleware>());
        Assert.Single(provider.GetServices<IChatTurnMiddleware>());
        Assert.Single(provider.GetServices<IStreamingChatTurnMiddleware>());
        Assert.Single(provider.GetServices<IToolExecutionMiddleware>());
    }

    [Fact]
    public async Task ChatSessionBuilder_UseServiceProvider_ShouldApplyRegisteredMiddleware()
    {
        var services = new ServiceCollection();
        services.AddAgileAI();
        services.AddChatTurnMiddleware<RecordingChatTurnMiddleware>();

        var chatClient = new Mock<IChatClient>();
        chatClient
            .Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = ChatMessage.Assistant("done")
            });

        services.AddSingleton(chatClient.Object);
        services.AddSingleton<IChatClient>(chatClient.Object);

        var provider = services.BuildServiceProvider();

        var session = new ChatSessionBuilder(chatClient.Object, "test-model")
            .UseServiceProvider(provider)
            .Build();

        var result = await session.SendTurnAsync("hello");

        Assert.Equal("done|turn-after", result.Response.Message?.TextContent);
    }

    [Fact]
    public async Task LoggingChatTurnMiddleware_ShouldLogAndPreserveResult()
    {
        var logger = new ListLogger<LoggingChatTurnMiddleware>();
        var middleware = new LoggingChatTurnMiddleware(logger, new LoggingMiddlewareOptions { LogInputs = true });
        var chatClient = new Mock<IChatClient>();
        chatClient.Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = ChatMessage.Assistant("done")
            });

        var session = new ChatSession(chatClient.Object, "test-model", chatTurnMiddlewares: [middleware]);

        var result = await session.SendTurnAsync("hello");

        Assert.Equal("done", result.Response.Message?.TextContent);
        Assert.Contains(logger.Messages, x => x.Contains("Starting chat turn"));
        Assert.Contains(logger.Messages, x => x.Contains("Completed chat turn"));
    }

    [Fact]
    public async Task ToolPolicyMiddleware_ShouldDenyBlockedToolBeforeExecution()
    {
        var chatClient = new Mock<IChatClient>();
        var toolCall = new ToolCall { Id = "call-policy", Name = "dangerous-tool", Arguments = "{}" };
        chatClient.SetupSequence(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = new ChatMessage
                {
                    Role = ChatRole.Assistant,
                    ToolCalls = [toolCall]
                }
            })
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = ChatMessage.Assistant("fallback")
            });

        var tool = new CountingTool("dangerous-tool", "should-not-run");
        var registry = new InMemoryToolRegistry();
        registry.Register(tool);

        var session = new ChatSession(
            chatClient.Object,
            "test-model",
            registry,
            toolExecutionMiddlewares:
            [
                new ToolPolicyMiddleware(new ToolPolicyOptions
                {
                    DeniedToolNames = ["dangerous-tool"]
                })
            ]);

        var result = await session.SendTurnAsync("run dangerous tool");

        Assert.Equal(0, tool.ExecutionCount);
        Assert.NotNull(result.ToolResults);
        var toolResult = Assert.Single(result.ToolResults!);
        Assert.False(toolResult.IsSuccess);
        Assert.Equal(ToolExecutionStatus.Denied, toolResult.Status);
        Assert.Contains("denied by policy", toolResult.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(session.History, x =>
            x.Role == ChatRole.Tool &&
            x.TextContent is not null &&
            x.TextContent.Contains("denied by policy", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RecordingAgentExecutionMiddleware : IAgentExecutionMiddleware
    {
        public List<string> Events { get; } = [];

        public async Task<AgentResult> InvokeAsync(AgentExecutionContext context, Func<Task<AgentResult>> next, CancellationToken cancellationToken = default)
        {
            Events.Add($"before:{context.Request.Input}");
            var result = await next();
            Events.Add($"after:{result.Output}");
            return result with { Output = $"{result.Output}|agent-after" };
        }
    }

    private sealed class RecordingChatTurnMiddleware : IChatTurnMiddleware
    {
        public List<string> Events { get; } = [];
        public List<ChatTurnExecutionKind> SeenKinds { get; } = [];

        public async Task<ChatTurnResult> InvokeAsync(ChatTurnExecutionContext context, Func<Task<ChatTurnResult>> next, CancellationToken cancellationToken = default)
        {
            SeenKinds.Add(context.Kind);
            Events.Add($"before:{context.Input ?? "continue"}");
            var result = await next();
            Events.Add($"after:{result.Response.Message?.TextContent ?? result.Response.ErrorMessage ?? string.Empty}");
            return result with
            {
                Response = result.Response with
                {
                    Message = result.Response.Message is null
                        ? null
                        : result.Response.Message with { TextContent = $"{result.Response.Message.TextContent}|turn-after" }
                }
            };
        }
    }

    private sealed class ShortCircuitChatTurnMiddleware(string output) : IChatTurnMiddleware
    {
        public List<ChatTurnExecutionKind> SeenKinds { get; } = [];

        public Task<ChatTurnResult> InvokeAsync(ChatTurnExecutionContext context, Func<Task<ChatTurnResult>> next, CancellationToken cancellationToken = default)
        {
            SeenKinds.Add(context.Kind);
            return Task.FromResult(new ChatTurnResult
            {
                Response = new ChatResponse
                {
                    IsSuccess = true,
                    Message = ChatMessage.Assistant(output)
                }
            });
        }
    }

    private sealed class RecordingToolExecutionMiddleware : IToolExecutionMiddleware
    {
        public List<string> Events { get; } = [];

        public async Task<ToolExecutionOutcome> InvokeAsync(ToolExecutionMiddlewareContext context, Func<Task<ToolExecutionOutcome>> next, CancellationToken cancellationToken = default)
        {
            Events.Add($"before:{context.Tool.Name}");
            var outcome = await next();
            Events.Add($"after:{outcome.Result.Content}");
            return outcome with
            {
                Result = outcome.Result with { Content = $"{outcome.Result.Content}|tool-after" }
            };
        }
    }

    private sealed class PassthroughStreamingMiddleware : IStreamingChatTurnMiddleware
    {
        public IAsyncEnumerable<ChatTurnStreamUpdate> InvokeAsync(
            StreamingChatTurnExecutionContext context,
            Func<IAsyncEnumerable<ChatTurnStreamUpdate>> next,
            CancellationToken cancellationToken = default)
            => next();
    }

    private sealed class DelegateTool(string name, Func<ToolExecutionContext, Task<ToolResult>> handler) : ITool
    {
        public string Name => name;
        public string? Description => name;
        public object? ParametersSchema => new { };

        public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
            => handler(context);
    }

    private sealed class CountingTool(string name, string content) : ITool
    {
        public string Name => name;
        public string? Description => name;
        public object? ParametersSchema => new { };
        public int ExecutionCount { get; private set; }

        public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult(new ToolResult
            {
                ToolCallId = context.ToolCall.Id,
                Content = content,
                IsSuccess = true
            });
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
