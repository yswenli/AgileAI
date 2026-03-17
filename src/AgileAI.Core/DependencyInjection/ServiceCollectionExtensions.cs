using Microsoft.Extensions.DependencyInjection;
using AgileAI.Abstractions;
using AgileAI.Core;

namespace AgileAI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgileAI(this IServiceCollection services)
    {
        services.AddSingleton<ChatClient>();
        services.AddSingleton<IChatClient>(sp => sp.GetRequiredService<ChatClient>());
        services.AddSingleton<IToolRegistry, InMemoryToolRegistry>();
        services.AddSingleton<ISkillRegistry, InMemorySkillRegistry>();
        
        return services;
    }

    public static IServiceCollection AddChatClientProvider(this IServiceCollection services, IChatModelProvider provider)
    {
        services.AddSingleton(provider);
        return services;
    }
}
