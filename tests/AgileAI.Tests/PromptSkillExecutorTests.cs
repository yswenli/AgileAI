using AgileAI.Abstractions;
using AgileAI.Core;
using Moq;

namespace AgileAI.Tests;

public class PromptSkillExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldInjectSkillPromptAndReturnResponse()
    {
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient
            .Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = ChatMessage.Assistant("done")
            });

        var executor = new PromptSkillExecutor(mockChatClient.Object);
        var manifest = new SkillManifest
        {
            Name = "weather",
            Description = "Weather helper",
            RootDirectory = "/tmp/weather",
            SkillMarkdownPath = "/tmp/weather/SKILL.md",
            InstructionBody = "Always answer with forecast first."
        };

        var result = await executor.ExecuteAsync(manifest, new SkillExecutionContext
        {
            Request = new AgentRequest
            {
                Input = "what's the weather",
                ModelId = "openai:gpt-4o"
            },
            ModelId = "openai:gpt-4o"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("done", result.Output);
        mockChatClient.Verify(x => x.CompleteAsync(
            It.Is<ChatRequest>(r =>
                r.Messages.Count >= 2 &&
                r.Messages[0].Role == ChatRole.System &&
                r.Messages[0].TextContent != null &&
                r.Messages[0].TextContent.Contains("AgileAI local skill: weather") &&
                r.Messages[1].Role == ChatRole.User),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeduplicateExistingGeneratedSkillPromptFromHistory()
    {
        var mockChatClient = new Mock<IChatClient>();
        ChatRequest? capturedRequest = null;

        mockChatClient
            .Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ChatRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new ChatResponse
            {
                IsSuccess = true,
                Message = ChatMessage.Assistant("done")
            });

        var executor = new PromptSkillExecutor(mockChatClient.Object);
        var manifest = new SkillManifest
        {
            Name = "weather",
            Description = "Weather helper",
            RootDirectory = "/tmp/weather",
            SkillMarkdownPath = "/tmp/weather/SKILL.md",
            InstructionBody = "Always answer with forecast first."
        };

        var history = new List<ChatMessage>
        {
            ChatMessage.System(SkillPromptHelper.BuildSystemPrompt(manifest)),
            ChatMessage.User("old question"),
            ChatMessage.Assistant("old answer")
        };

        var result = await executor.ExecuteAsync(manifest, new SkillExecutionContext
        {
            Request = new AgentRequest
            {
                Input = "new question",
                ModelId = "openai:gpt-4o",
                History = history
            },
            ModelId = "openai:gpt-4o"
        });

        Assert.NotNull(capturedRequest);
        var requestMessages = capturedRequest.Messages;
        Assert.Single(requestMessages, m => SkillPromptHelper.IsGeneratedSkillPromptFor(m, "weather"));
        Assert.NotNull(result.UpdatedHistory);
        Assert.Equal(5, result.UpdatedHistory.Count);
        Assert.Single(result.UpdatedHistory, m => SkillPromptHelper.IsGeneratedSkillPromptFor(m, "weather"));
    }
}
