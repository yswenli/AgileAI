using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AgileAI.Providers.Gemini;
using AgileAI.Providers.Gemini.DependencyInjection;

namespace AgileAI.Tests;

public class GeminiDependencyInjectionTests
{
    [Fact]
    public void AddGeminiProvider_ShouldConfigureOptions()
    {
        var services = new ServiceCollection();
        
        services.AddGeminiProvider(options =>
        {
            options.ApiKey = "test-gemini-key";
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<GeminiOptions>>();
        
        Assert.Equal("test-gemini-key", optionsSnapshot.Value.ApiKey);
    }
}
