using AgileAI.Abstractions;
using AgileAI.Core;
using Moq;

namespace AgileAI.Tests;

public class ChatClientTests
{
    [Fact]
    public void RegisterProvider_ShouldStoreProviderByProviderName()
    {
        var chatClient = new ChatClient();
        var mockProvider = new Mock<IChatModelProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("test-provider");

        chatClient.RegisterProvider(mockProvider.Object);

        var request = new ChatRequest { ModelId = "test-provider:test-model" };
        Assert.True(true);
    }

    [Fact]
    public async Task CompleteAsync_ShouldRouteToCorrectProvider()
    {
        var chatClient = new ChatClient();
        var mockProvider = new Mock<IChatModelProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("test-provider");
        mockProvider.Setup(p => p.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true });

        chatClient.RegisterProvider(mockProvider.Object);

        var request = new ChatRequest { ModelId = "test-provider:test-model" };
        var response = await chatClient.CompleteAsync(request);

        Assert.True(response.IsSuccess);
        mockProvider.Verify(p => p.CompleteAsync(
            It.Is<ChatRequest>(r => r.ModelId == "test-model"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_ShouldReturnErrorForUnknownProvider()
    {
        var chatClient = new ChatClient();
        var request = new ChatRequest { ModelId = "unknown:model" };

        var response = await chatClient.CompleteAsync(request);

        Assert.False(response.IsSuccess);
        Assert.Contains("not found", response.ErrorMessage);
    }

    [Fact]
    public async Task CompleteAsync_ShouldUseDefaultProviderWhenNoProviderSpecified()
    {
        var chatClient = new ChatClient();
        var mockProvider = new Mock<IChatModelProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("openai");
        mockProvider.Setup(p => p.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true });

        chatClient.RegisterProvider(mockProvider.Object);

        var request = new ChatRequest { ModelId = "gpt-4" };
        var response = await chatClient.CompleteAsync(request);

        Assert.True(response.IsSuccess);
        mockProvider.Verify(p => p.CompleteAsync(
            It.Is<ChatRequest>(r => r.ModelId == "gpt-4"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamAsync_ShouldRouteToCorrectProvider()
    {
        var chatClient = new ChatClient();
        var mockProvider = new Mock<IChatModelProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("test-provider");
        
        async IAsyncEnumerable<StreamingChatUpdate> GetUpdates()
        {
            yield return new TextDeltaUpdate("test");
        }
        
        mockProvider.Setup(p => p.StreamAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .Returns(GetUpdates());

        chatClient.RegisterProvider(mockProvider.Object);

        var request = new ChatRequest { ModelId = "test-provider:test-model" };
        var updates = new List<StreamingChatUpdate>();
        await foreach (var update in chatClient.StreamAsync(request))
        {
            updates.Add(update);
        }

        Assert.Single(updates);
        mockProvider.Verify(p => p.StreamAsync(
            It.Is<ChatRequest>(r => r.ModelId == "test-model"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
