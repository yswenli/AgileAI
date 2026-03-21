using Moq;
using AgileAI.Abstractions;
using AgileAI.Core;
using AgileAI.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace AgileAI.Tests;

public class ChatClientMultiProviderTests
{
    [Fact]
    public async Task ChatClient_WithMultipleProviders_ShouldRouteToCorrectProvider()
    {
        var services = new ServiceCollection();
        
        var mockProvider1 = new Mock<IChatModelProvider>();
        mockProvider1.Setup(p => p.ProviderName).Returns("provider1");
        mockProvider1.Setup(p => p.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = ChatMessage.Assistant("From provider 1") });
        
        var mockProvider2 = new Mock<IChatModelProvider>();
        mockProvider2.Setup(p => p.ProviderName).Returns("provider2");
        mockProvider2.Setup(p => p.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = ChatMessage.Assistant("From provider 2") });
        
        services.AddSingleton<IChatModelProvider>(mockProvider1.Object);
        services.AddSingleton<IChatModelProvider>(mockProvider2.Object);
        services.AddAgileAI();
        
        var serviceProvider = services.BuildServiceProvider();
        var chatClient = serviceProvider.GetRequiredService<IChatClient>();
        
        var request1 = new ChatRequest { ModelId = "provider1:model-a", Messages = [ChatMessage.User("test")] };
        var result1 = await chatClient.CompleteAsync(request1);
        Assert.Equal("From provider 1", result1.Message?.TextContent);
        
        var request2 = new ChatRequest { ModelId = "provider2:model-b", Messages = [ChatMessage.User("test")] };
        var result2 = await chatClient.CompleteAsync(request2);
        Assert.Equal("From provider 2", result2.Message?.TextContent);
    }

    [Fact]
    public async Task ChatClient_WithUnknownProvider_ShouldReturnError()
    {
        var services = new ServiceCollection();
        
        var mockProvider = new Mock<IChatModelProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("known-provider");
        services.AddSingleton<IChatModelProvider>(mockProvider.Object);
        services.AddAgileAI();
        
        var serviceProvider = services.BuildServiceProvider();
        var chatClient = serviceProvider.GetRequiredService<IChatClient>();
        
        var request = new ChatRequest { ModelId = "unknown-provider:model-x", Messages = [ChatMessage.User("test")] };
        var result = await chatClient.CompleteAsync(request);
        
        Assert.False(result.IsSuccess);
        Assert.Contains("Provider 'unknown-provider' not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ChatClient_WithUnprefixedModel_ShouldDefaultToOpenAI()
    {
        var services = new ServiceCollection();
        
        var mockProvider = new Mock<IChatModelProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("openai");
        mockProvider.Setup(p => p.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Message = ChatMessage.Assistant("From default openai") });
        
        services.AddSingleton<IChatModelProvider>(mockProvider.Object);
        services.AddAgileAI();
        
        var serviceProvider = services.BuildServiceProvider();
        var chatClient = serviceProvider.GetRequiredService<IChatClient>();
        
        var request = new ChatRequest { ModelId = "gpt-4", Messages = [ChatMessage.User("test")] };
        var result = await chatClient.CompleteAsync(request);
        
        Assert.Equal("From default openai", result.Message?.TextContent);
        mockProvider.Verify(p => p.CompleteAsync(It.Is<ChatRequest>(r => r.ModelId == "gpt-4"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
