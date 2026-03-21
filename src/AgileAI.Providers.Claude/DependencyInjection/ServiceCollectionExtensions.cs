using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AgileAI.Abstractions;
using AgileAI.Core;
using AgileAI.Providers.Claude;

namespace AgileAI.Providers.Claude.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClaudeProvider(this IServiceCollection services, Action<ClaudeOptions> configureOptions)
    {
        services.Configure(configureOptions);
        
        services.AddHttpClient<ClaudeChatModelProvider>()
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ClaudeOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<ClaudeRetryHttpMessageHandler>>();
                return new ClaudeRetryHttpMessageHandler(options, logger);
            });
        
        services.AddSingleton<IChatModelProvider>(sp => sp.GetRequiredService<ClaudeChatModelProvider>());
        
        return services;
    }
}
