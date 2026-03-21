using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AgileAI.Abstractions;
using AgileAI.Core;
using AgileAI.Providers.OpenAIResponses;

namespace AgileAI.Providers.OpenAIResponses.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAIResponsesProvider(this IServiceCollection services, Action<OpenAIResponsesOptions> configureOptions)
    {
        services.Configure(configureOptions);
        
        services.AddHttpClient<OpenAIResponsesChatModelProvider>()
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<OpenAIResponsesOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<OpenAIResponsesRetryHttpMessageHandler>>();
                return new OpenAIResponsesRetryHttpMessageHandler(options, logger);
            });
        
        services.AddSingleton<IChatModelProvider>(sp => sp.GetRequiredService<OpenAIResponsesChatModelProvider>());
        
        return services;
    }
}
