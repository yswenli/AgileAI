using AgileAI.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgileAI.Providers.OpenAICompatible.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAICompatibleProvider(this IServiceCollection services, Action<OpenAICompatibleOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddHttpClient<OpenAICompatibleChatModelProvider>()
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<OpenAICompatibleOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<OpenAICompatibleRetryHttpMessageHandler>>();
                return new OpenAICompatibleRetryHttpMessageHandler(options, logger);
            });

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OpenAICompatibleOptions>>().Value);
        services.AddSingleton<IChatModelProvider>(sp => sp.GetRequiredService<OpenAICompatibleChatModelProvider>());

        return services;
    }
}
