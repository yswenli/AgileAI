using AgileAI.Abstractions;
using AgileAI.DependencyInjection;
using AgileAI.Providers.OpenAICompatible;
using AgileAI.Providers.OpenAICompatible.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgileAI.Tests;

public class OpenAICompatibleDependencyInjectionTests
{
    [Fact]
    public void AddOpenAICompatibleProvider_ShouldConfigureOptions()
    {
        var services = new ServiceCollection();

        services.AddOpenAICompatibleProvider(options =>
        {
            options.ProviderName = "deepseek";
            options.ApiKey = "test-key-123";
            options.BaseUrl = "https://example.com/v1/";
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<OpenAICompatibleOptions>>();

        Assert.Equal("deepseek", optionsSnapshot.Value.ProviderName);
        Assert.Equal("test-key-123", optionsSnapshot.Value.ApiKey);
        Assert.Equal("https://example.com/v1/", optionsSnapshot.Value.BaseUrl);
    }

    [Fact]
    public void AddOpenAICompatibleProvider_WithAddAgileAI_ShouldResolveProviderAndRouteRequests()
    {
        var services = new ServiceCollection();

        services.AddOpenAICompatibleProvider(options =>
        {
            options.ProviderName = "deepseek";
            options.ApiKey = "test-key-123";
            options.BaseUrl = "https://example.com/v1/";
        });
        services.AddAgileAI();

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetServices<IChatModelProvider>().Single();
        var chatClient = serviceProvider.GetRequiredService<IChatClient>();

        Assert.Equal("deepseek", provider.ProviderName);
        Assert.NotNull(chatClient);
    }
}
