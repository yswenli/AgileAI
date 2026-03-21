using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AgileAI.Providers.OpenAIResponses;
using AgileAI.Providers.OpenAIResponses.DependencyInjection;

namespace AgileAI.Tests;

public class OpenAIResponsesDependencyInjectionTests
{
    [Fact]
    public void AddOpenAIResponsesProvider_ShouldConfigureOptions()
    {
        var services = new ServiceCollection();
        
        services.AddOpenAIResponsesProvider(options =>
        {
            options.ApiKey = "test-key-123";
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<OpenAIResponsesOptions>>();
        
        Assert.Equal("test-key-123", optionsSnapshot.Value.ApiKey);
    }
}
