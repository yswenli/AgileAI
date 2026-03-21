using AgileAI.Studio.Api.Domain;
using AgileAI.Providers.OpenAICompatible;

namespace AgileAI.Studio.Api.Services;

public record ProviderRuntimeOptions(
    ProviderType ProviderType,
    string RuntimeProviderName,
    string RuntimeModelId,
    string ApiKey,
    string? BaseUrl,
    string? Endpoint,
    string? RelativePath,
    OpenAICompatibleAuthMode? AuthMode,
    string? ApiKeyHeaderName,
    string? ApiVersion);
