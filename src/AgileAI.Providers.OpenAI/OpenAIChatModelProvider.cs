using AgileAI.Abstractions;
using AgileAI.Core;
using Microsoft.Extensions.Logging;

namespace AgileAI.Providers.OpenAI;

public class OpenAIChatModelProvider : OpenAICompatibleProviderBase
{
    public override string ProviderName => "openai";

    public OpenAIChatModelProvider(
        HttpClient httpClient,
        OpenAIOptions options,
        ILogger<OpenAIChatModelProvider>? logger = null)
        : base(httpClient, logger)
    {
        httpClient.BaseAddress = new Uri(options.BaseUrl ?? "https://api.openai.com/v1/");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
    }

    protected override object CreateProviderRequest(ChatRequest request, bool stream)
        => CreateBaseRequest(request, stream, includeModel: true);

    protected override string BuildRelativeUrl(string modelOrDeployment)
        => "chat/completions";

    protected override string GetInvalidResponseMessage()
        => "Invalid response from OpenAI";
}
