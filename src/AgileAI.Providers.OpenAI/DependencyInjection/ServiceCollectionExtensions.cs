using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AgileAI.Abstractions;
using AgileAI.Core;
using AgileAI.Providers.OpenAI;

namespace AgileAI.Providers.OpenAI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAIProvider(this IServiceCollection services, Action<OpenAIOptions> configureOptions)
    {
        services.Configure(configureOptions);
        
        services.AddHttpClient<OpenAIChatModelProvider>()
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<RetryHttpMessageHandler>>();
                return new RetryHttpMessageHandler(options, logger);
            });
        
        services.AddSingleton<IChatModelProvider>(sp => sp.GetRequiredService<OpenAIChatModelProvider>());
        
        services.AddSingleton<ChatClient>(sp =>
        {
            var chatClient = new ChatClient();
            var provider = sp.GetRequiredService<IChatModelProvider>();
            chatClient.RegisterProvider(provider);
            return chatClient;
        });
        
        return services;
    }
}
