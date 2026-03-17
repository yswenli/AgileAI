using AgileAI.Abstractions;
using AgileAI.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgileAI.Providers.AzureOpenAI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureOpenAIProvider(this IServiceCollection services, Action<AzureOpenAIOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddHttpClient<AzureOpenAIChatModelProvider>()
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<AzureOpenAIRetryHttpMessageHandler>>();
                return new AzureOpenAIRetryHttpMessageHandler(options, logger);
            });

        services.AddSingleton<IChatModelProvider>(sp => sp.GetRequiredService<AzureOpenAIChatModelProvider>());

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
