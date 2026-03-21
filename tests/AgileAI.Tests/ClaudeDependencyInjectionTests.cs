using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AgileAI.Providers.Claude;
using AgileAI.Providers.Claude.DependencyInjection;

namespace AgileAI.Tests;

public class ClaudeDependencyInjectionTests
{
    [Fact]
    public void AddClaudeProvider_ShouldConfigureOptions()
    {
        var services = new ServiceCollection();
        
        services.AddClaudeProvider(options =>
        {
            options.ApiKey = "test-claude-key";
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<ClaudeOptions>>();
        
        Assert.Equal("test-claude-key", optionsSnapshot.Value.ApiKey);
    }
}
