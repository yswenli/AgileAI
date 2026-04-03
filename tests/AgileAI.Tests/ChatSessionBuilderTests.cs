using AgileAI.Abstractions;
using AgileAI.Core;
using AgileAI.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AgileAI.Tests;

public class ChatSessionBuilderTests
{
    [Fact]
    public async Task Build_WithHistory_ShouldSeedSessionHistory()
    {
        var client = new Mock<IChatClient>();
        client.Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = ChatMessage.Assistant("done") });

        var session = new ChatSessionBuilder(client.Object, "test-model")
            .WithHistory([
                ChatMessage.System("system"),
                ChatMessage.User("existing")
            ])
            .Build();

        await session.SendAsync("new message");

        Assert.Equal(4, session.History.Count);
        Assert.Equal("system", session.History[0].TextContent);
        Assert.Equal("existing", session.History[1].TextContent);
    }

    [Fact]
    public async Task Build_WithToolRegistry_ShouldExecuteRegisteredTool()
    {
        var toolCall = new ToolCall { Id = "call-1", Name = "builder-tool", Arguments = "{}" };
        var client = new Mock<IChatClient>();
        client.SetupSequence(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = new ChatMessage
                {
                    Role = ChatRole.Assistant,
                    ToolCalls = [toolCall]
                }
            })
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = ChatMessage.Assistant("final") });

        var tool = new Mock<ITool>();
        tool.SetupGet(x => x.Name).Returns("builder-tool");
        tool.Setup(x => x.ExecuteAsync(It.IsAny<ToolExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolResult { ToolCallId = "call-1", Content = "tool-output", IsSuccess = true });

        var registry = new InMemoryToolRegistry();
        registry.Register(tool.Object);

        var session = new ChatSessionBuilder(client.Object, "test-model")
            .WithToolRegistry(registry)
            .Build();

        await session.SendAsync("run tool");

        tool.Verify(x => x.ExecuteAsync(It.IsAny<ToolExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(session.History, x => x.Role == ChatRole.Tool && x.TextContent == "tool-output");
    }

    [Fact]
    public async Task UseServiceProvider_ShouldResolveRegisteredChatTurnMiddleware()
    {
        var services = new ServiceCollection();
        var client = new Mock<IChatClient>();
        client.Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = ChatMessage.Assistant("done") });

        services.AddChatTurnMiddleware<BuilderChatTurnMiddleware>();
        var provider = services.BuildServiceProvider();

        var session = new ChatSessionBuilder(client.Object, "test-model")
            .UseServiceProvider(provider)
            .Build();

        var result = await session.SendTurnAsync("hello");

        Assert.Equal("done|builder", result.Response.Message?.TextContent);
    }

    private sealed class BuilderChatTurnMiddleware : IChatTurnMiddleware
    {
        public async Task<ChatTurnResult> InvokeAsync(ChatTurnExecutionContext context, Func<Task<ChatTurnResult>> next, CancellationToken cancellationToken = default)
        {
            var result = await next();
            return result with
            {
                Response = result.Response with
                {
                    Message = result.Response.Message is null
                        ? null
                        : result.Response.Message with { TextContent = $"{result.Response.Message.TextContent}|builder" }
                }
            };
        }
    }
}
