using Moq;
using Microsoft.Extensions.DependencyInjection;
using AgileAI.Abstractions;
using AgileAI.Core;
using AgileAI.DependencyInjection;

namespace AgileAI.Tests;

public class CoreDependencyInjectionTests
{
    [Fact]
    public void AddAgileAI_ShouldRegisterAllServices()
    {
        var services = new ServiceCollection();

        services.AddAgileAI();

        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<IChatClient>());
        Assert.NotNull(serviceProvider.GetService<ChatClient>());
        Assert.NotNull(serviceProvider.GetService<IToolRegistry>());
        Assert.NotNull(serviceProvider.GetService<ISkillRegistry>());
        Assert.NotNull(serviceProvider.GetService<ISkillPlanner>());
        Assert.NotNull(serviceProvider.GetService<ISkillContinuationPolicy>());
        Assert.NotNull(serviceProvider.GetService<ISessionStore>());
        Assert.NotNull(serviceProvider.GetService<IAgentRuntime>());
    }

    [Fact]
    public void AddChatClientProvider_ShouldRegisterProvider()
    {
        var services = new ServiceCollection();
        var mockProvider = new Mock<IChatModelProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("test-provider");

        services.AddChatClientProvider(mockProvider.Object);

        var serviceProvider = services.BuildServiceProvider();
        var registeredProvider = serviceProvider.GetService<IChatModelProvider>();

        Assert.NotNull(registeredProvider);
        Assert.Equal("test-provider", registeredProvider.ProviderName);
    }

    [Fact]
    public void AddFileSessionStore_ShouldReplaceDefaultSessionStore()
    {
        var services = new ServiceCollection();
        var tempDirectory = Path.Combine(Path.GetTempPath(), "AgileAI.Tests", Guid.NewGuid().ToString("N"));

        services.AddAgileAI();
        services.AddFileSessionStore(options => options.RootDirectory = tempDirectory);

        var serviceProvider = services.BuildServiceProvider();
        var sessionStore = serviceProvider.GetRequiredService<ISessionStore>();

        Assert.IsType<FileSessionStore>(sessionStore);
        Assert.Equal(tempDirectory, serviceProvider.GetRequiredService<FileSessionStoreOptions>().RootDirectory);

        Directory.Delete(tempDirectory, recursive: true);
    }

    [Fact]
    public void AddBuiltInMiddlewareHelpers_ShouldRegisterBuiltInImplementations()
    {
        var services = new ServiceCollection();

        services.AddAgileAI();
        services.AddLoggingChatTurnMiddleware(options => options.LogInputs = true);
        services.AddLoggingStreamingChatTurnMiddleware();
        services.AddLoggingToolExecutionMiddleware(options => options.LogToolArguments = true);
        services.AddToolPolicyMiddleware(options => options.DeniedToolNames = ["dangerous-tool"]);

        var serviceProvider = services.BuildServiceProvider();

        Assert.Contains(serviceProvider.GetServices<IChatTurnMiddleware>(), x => x is LoggingChatTurnMiddleware);
        Assert.Contains(serviceProvider.GetServices<IStreamingChatTurnMiddleware>(), x => x is LoggingStreamingChatTurnMiddleware);
        Assert.Contains(serviceProvider.GetServices<IToolExecutionMiddleware>(), x => x is LoggingToolExecutionMiddleware);
        Assert.Contains(serviceProvider.GetServices<IToolExecutionMiddleware>(), x => x is ToolPolicyMiddleware);

        var loggingOptions = serviceProvider.GetRequiredService<LoggingMiddlewareOptions>();
        Assert.True(loggingOptions.LogInputs || loggingOptions.LogToolArguments);

        var policyOptions = serviceProvider.GetRequiredService<ToolPolicyOptions>();
        Assert.Contains("dangerous-tool", policyOptions.DeniedToolNames!);
    }
}
