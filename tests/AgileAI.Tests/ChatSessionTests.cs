using AgileAI.Abstractions;
using AgileAI.Core;
using Moq;

namespace AgileAI.Tests;

public class ChatSessionTests
{
    [Fact]
    public async Task SendAsync_ShouldAddUserMessageToHistory()
    {
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient.Setup(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = ChatMessage.Assistant("response") });

        var session = new ChatSession(mockChatClient.Object, "test-model");

        await session.SendAsync("hello");

        Assert.Equal(2, session.History.Count);
        Assert.Equal(ChatRole.User, session.History[0].Role);
        Assert.Equal("hello", session.History[0].TextContent);
    }

    [Fact]
    public async Task SendAsync_ShouldAddAssistantMessageToHistory()
    {
        var mockChatClient = new Mock<IChatClient>();
        var assistantMessage = ChatMessage.Assistant("response");
        mockChatClient.Setup(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = assistantMessage });

        var session = new ChatSession(mockChatClient.Object, "test-model");

        await session.SendAsync("hello");

        Assert.Equal(2, session.History.Count);
        Assert.Equal(ChatRole.Assistant, session.History[1].Role);
        Assert.Equal("response", session.History[1].TextContent);
    }

    [Fact]
    public async Task SendAsync_WithToolCalls_ShouldExecuteTools()
    {
        var toolCallId = "call_123";
        var toolName = "test-tool";
        var mockChatClient = new Mock<IChatClient>();
        var toolCall = new ToolCall { Id = toolCallId, Name = toolName, Arguments = "{}" };
        var assistantMessage = new ChatMessage 
        { 
            Role = ChatRole.Assistant, 
            ToolCalls = new List<ToolCall> { toolCall } 
        };

        mockChatClient.SetupSequence(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = assistantMessage })
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = ChatMessage.Assistant("final response") });

        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns(toolName);
        mockTool.Setup(t => t.ExecuteAsync(It.IsAny<ToolExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolResult { ToolCallId = toolCallId, Content = "tool result", IsSuccess = true });

        var toolRegistry = new InMemoryToolRegistry();
        toolRegistry.Register(mockTool.Object);

        var session = new ChatSession(mockChatClient.Object, "test-model", toolRegistry);
        var response = await session.SendAsync("hello");

        Assert.True(response.IsSuccess);
        mockTool.Verify(t => t.ExecuteAsync(It.IsAny<ToolExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(4, session.History.Count);
        Assert.Equal(ChatRole.Tool, session.History[2].Role);
        Assert.Equal("tool result", session.History[2].TextContent);
    }

    [Fact]
    public async Task SendAsync_WithUnknownTool_ShouldReturnErrorToolResult()
    {
        var toolCallId = "call_123";
        var toolName = "unknown-tool";
        var mockChatClient = new Mock<IChatClient>();
        var toolCall = new ToolCall { Id = toolCallId, Name = toolName, Arguments = "{}" };
        var assistantMessage = new ChatMessage 
        { 
            Role = ChatRole.Assistant, 
            ToolCalls = new List<ToolCall> { toolCall } 
        };

        mockChatClient.SetupSequence(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = assistantMessage })
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = ChatMessage.Assistant("final response") });

        var toolRegistry = new InMemoryToolRegistry();

        var session = new ChatSession(mockChatClient.Object, "test-model", toolRegistry);
        await session.SendAsync("hello");

        Assert.Equal(4, session.History.Count);
        Assert.Equal(ChatRole.Tool, session.History[2].Role);
        Assert.Contains("not found", session.History[2].TextContent);
    }

    [Fact]
    public void AddMessage_ShouldAddToHistory()
    {
        var mockChatClient = new Mock<IChatClient>();
        var session = new ChatSession(mockChatClient.Object, "test-model");
        var message = ChatMessage.User("test message");

        session.AddMessage(message);

        Assert.Single(session.History);
        Assert.Equal(message, session.History[0]);
    }

    [Fact]
    public void ClearHistory_ShouldEmptyHistory()
    {
        var mockChatClient = new Mock<IChatClient>();
        var session = new ChatSession(mockChatClient.Object, "test-model");
        session.AddMessage(ChatMessage.User("test"));

        session.ClearHistory();

        Assert.Empty(session.History);
    }

    [Fact]
    public async Task SendAsync_WithToolThrowingException_ShouldReturnErrorToolResult()
    {
        var toolCallId = "call_123";
        var toolName = "error-tool";
        var mockChatClient = new Mock<IChatClient>();
        var toolCall = new ToolCall { Id = toolCallId, Name = toolName, Arguments = "{}" };
        var assistantMessage = new ChatMessage 
        { 
            Role = ChatRole.Assistant, 
            ToolCalls = new List<ToolCall> { toolCall } 
        };

        mockChatClient.SetupSequence(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = assistantMessage })
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = ChatMessage.Assistant("final response") });

        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns(toolName);
        mockTool.Setup(t => t.ExecuteAsync(It.IsAny<ToolExecutionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var toolRegistry = new InMemoryToolRegistry();
        toolRegistry.Register(mockTool.Object);

        var session = new ChatSession(mockChatClient.Object, "test-model", toolRegistry);
        await session.SendAsync("hello");

        Assert.Equal(4, session.History.Count);
        Assert.Equal(ChatRole.Tool, session.History[2].Role);
        Assert.Contains("Something went wrong", session.History[2].TextContent);
    }

    [Fact]
    public async Task SendAsync_WithMaxToolLoopIterationsReached_ShouldStop()
    {
        var toolCallId = "call_123";
        var toolName = "loop-tool";
        var mockChatClient = new Mock<IChatClient>();
        var toolCall = new ToolCall { Id = toolCallId, Name = toolName, Arguments = "{}" };
        var assistantMessageWithTool = new ChatMessage 
        { 
            Role = ChatRole.Assistant, 
            ToolCalls = new List<ToolCall> { toolCall } 
        };

        mockChatClient.Setup(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = assistantMessageWithTool });

        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns(toolName);
        mockTool.Setup(t => t.ExecuteAsync(It.IsAny<ToolExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolResult { ToolCallId = toolCallId, Content = "loop result", IsSuccess = true });

        var toolRegistry = new InMemoryToolRegistry();
        toolRegistry.Register(mockTool.Object);

        var maxIterations = 2;
        var session = new ChatSession(mockChatClient.Object, "test-model", toolRegistry, maxIterations);
        var response = await session.SendAsync("hello");

        Assert.True(response.IsSuccess);
        mockChatClient.Verify(c => c.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(maxIterations));
    }
}
