using AgileAI.Abstractions;
using AgileAI.Core;
using Microsoft.Extensions.Logging;

namespace AgileAI.Providers.OpenAICompatible;

public class OpenAICompatibleChatModelProvider : OpenAICompatibleProviderBase
{
    private readonly OpenAICompatibleOptions _options;

    public override string ProviderName => _options.ProviderName;

    public OpenAICompatibleChatModelProvider(
        HttpClient httpClient,
        OpenAICompatibleOptions options,
        ILogger<OpenAICompatibleChatModelProvider>? logger = null)
        : base(httpClient, logger)
    {
        _options = options;

        ValidateOptions(options);

        httpClient.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl));
        ConfigureAuthentication(httpClient, options);
    }

    protected override object CreateProviderRequest(ChatRequest request, bool stream)
        => CreateBaseRequest(request, stream, includeModel: true);

    protected override string BuildRelativeUrl(string modelOrDeployment)
        => _options.RelativePath;

    protected override string GetInvalidResponseMessage()
        => "Invalid response from OpenAI-compatible provider";

    private static void ValidateOptions(OpenAICompatibleOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ProviderName))
        {
            throw new ArgumentException("Provider name is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException("API key is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new ArgumentException("Base URL is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.RelativePath))
        {
            throw new ArgumentException("Relative path is required.", nameof(options));
        }

        if (options.AuthMode == OpenAICompatibleAuthMode.ApiKeyHeader && string.IsNullOrWhiteSpace(options.ApiKeyHeaderName))
        {
            throw new ArgumentException("API key header name is required for ApiKeyHeader auth mode.", nameof(options));
        }
    }

    private static string EnsureTrailingSlash(string baseUrl)
        => baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

    private static void ConfigureAuthentication(HttpClient httpClient, OpenAICompatibleOptions options)
    {
        httpClient.DefaultRequestHeaders.Authorization = null;

        if (!string.IsNullOrWhiteSpace(options.ApiKeyHeaderName))
        {
            httpClient.DefaultRequestHeaders.Remove(options.ApiKeyHeaderName);
        }

        if (options.AuthMode == OpenAICompatibleAuthMode.Bearer)
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            return;
        }

        httpClient.DefaultRequestHeaders.Add(options.ApiKeyHeaderName!, options.ApiKey);
    }
}
