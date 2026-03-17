using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using AgileAI.Abstractions;

namespace AgileAI.Core;

public class ChatClient : IChatClient
{
    private readonly Dictionary<string, IChatModelProvider> _providers = new();
    private readonly ILogger<ChatClient>? _logger;

    public ChatClient(ILogger<ChatClient>? logger = null)
    {
        _logger = logger;
    }

    public void RegisterProvider(IChatModelProvider provider)
    {
        _providers[provider.ProviderName.ToLowerInvariant()] = provider;
        _logger?.LogInformation("Registered provider: {ProviderName}", provider.ProviderName);
    }

    public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var (providerName, modelId) = ParseModelId(request.ModelId);
        _logger?.LogInformation("Routing request to provider {ProviderName} for model {ModelId}", providerName, modelId);
        
        if (!_providers.TryGetValue(providerName.ToLowerInvariant(), out var provider))
        {
            _logger?.LogError("Provider '{ProviderName}' not found", providerName);
            return new ChatResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Provider '{providerName}' not found"
            };
        }

        var adjustedRequest = request with { ModelId = modelId };
        return await provider.CompleteAsync(adjustedRequest, cancellationToken);
    }

    public async IAsyncEnumerable<StreamingChatUpdate> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (providerName, modelId) = ParseModelId(request.ModelId);
        _logger?.LogInformation("Routing streaming request to provider {ProviderName} for model {ModelId}", providerName, modelId);
        
        if (!_providers.TryGetValue(providerName.ToLowerInvariant(), out var provider))
        {
            _logger?.LogError("Provider '{ProviderName}' not found for streaming", providerName);
            yield break;
        }

        var adjustedRequest = request with { ModelId = modelId };
        await foreach (var update in provider.StreamAsync(adjustedRequest, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return update;
        }
    }

    private static (string ProviderName, string ModelId) ParseModelId(string modelId)
    {
        var parts = modelId.Split(':', 2);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }
        return ("openai", modelId);
    }
}
