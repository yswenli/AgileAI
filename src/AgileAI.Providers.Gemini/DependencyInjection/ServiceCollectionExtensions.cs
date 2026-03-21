using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AgileAI.Abstractions;
using AgileAI.Core;
using AgileAI.Providers.Gemini;

namespace AgileAI.Providers.Gemini.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGeminiProvider(this IServiceCollection services, Action<GeminiOptions> configureOptions)
    {
        services.Configure(configureOptions);
        
        services.AddHttpClient<GeminiChatModelProvider>()
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<GeminiRetryHttpMessageHandler>>();
                return new GeminiRetryHttpMessageHandler(options, logger);
            })
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
                client.DefaultRequestHeaders.Add("x-goog-api-key", options.ApiKey);
            });
        
        services.AddSingleton<IChatModelProvider>(sp => sp.GetRequiredService<GeminiChatModelProvider>());
        
        return services;
    }
}
