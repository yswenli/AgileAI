using AgileAI.Abstractions;
using AgileAI.Core;
using Microsoft.Extensions.Logging;

namespace AgileAI.Providers.AzureOpenAI;

public class AzureOpenAIChatModelProvider : OpenAICompatibleProviderBase
{
    public override string ProviderName => "azure-openai";

    private readonly AzureOpenAIOptions _options;

    public AzureOpenAIChatModelProvider(HttpClient httpClient, AzureOpenAIOptions options, ILogger<AzureOpenAIChatModelProvider>? logger = null)
        : base(httpClient, logger)
    {
        _options = options;

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new ArgumentException("Azure OpenAI endpoint is required.", nameof(options));
        }

        httpClient.BaseAddress = new Uri(EnsureTrailingSlash(options.Endpoint));
        httpClient.DefaultRequestHeaders.Remove("api-key");
        httpClient.DefaultRequestHeaders.Add("api-key", options.ApiKey);
    }

    protected override object CreateProviderRequest(ChatRequest request, bool stream)
        => CreateBaseRequest(request, stream, includeModel: false);

    protected override string BuildRelativeUrl(string modelOrDeployment)
        => $"openai/deployments/{Uri.EscapeDataString(modelOrDeployment)}/chat/completions?api-version={Uri.EscapeDataString(_options.ApiVersion)}";

    protected override string GetInvalidResponseMessage()
        => "Invalid response from Azure OpenAI";

    private static string EnsureTrailingSlash(string endpoint)
        => endpoint.EndsWith('/') ? endpoint : endpoint + "/";
}
