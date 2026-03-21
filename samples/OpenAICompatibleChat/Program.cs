using AgileAI.Abstractions;
using AgileAI.DependencyInjection;
using AgileAI.Providers.OpenAICompatible;
using AgileAI.Providers.OpenAICompatible.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var providerName = Environment.GetEnvironmentVariable("OPENAI_COMPATIBLE_PROVIDER")
    ?? throw new InvalidOperationException("Please set OPENAI_COMPATIBLE_PROVIDER.");
var apiKey = Environment.GetEnvironmentVariable("OPENAI_COMPATIBLE_API_KEY")
    ?? throw new InvalidOperationException("Please set OPENAI_COMPATIBLE_API_KEY.");
var baseUrl = Environment.GetEnvironmentVariable("OPENAI_COMPATIBLE_BASE_URL")
    ?? throw new InvalidOperationException("Please set OPENAI_COMPATIBLE_BASE_URL.");
var model = Environment.GetEnvironmentVariable("OPENAI_COMPATIBLE_MODEL")
    ?? throw new InvalidOperationException("Please set OPENAI_COMPATIBLE_MODEL.");
var relativePath = Environment.GetEnvironmentVariable("OPENAI_COMPATIBLE_RELATIVE_PATH") ?? "chat/completions";
var authModeValue = Environment.GetEnvironmentVariable("OPENAI_COMPATIBLE_AUTH_MODE") ?? "bearer";
var customHeaderName = Environment.GetEnvironmentVariable("OPENAI_COMPATIBLE_API_KEY_HEADER");

var authMode = authModeValue.Equals("header", StringComparison.OrdinalIgnoreCase)
    ? OpenAICompatibleAuthMode.ApiKeyHeader
    : OpenAICompatibleAuthMode.Bearer;

if (authMode == OpenAICompatibleAuthMode.ApiKeyHeader && string.IsNullOrWhiteSpace(customHeaderName))
{
    throw new InvalidOperationException("Please set OPENAI_COMPATIBLE_API_KEY_HEADER when OPENAI_COMPATIBLE_AUTH_MODE=header.");
}

var services = new ServiceCollection();
services.AddAgileAI();
services.AddOpenAICompatibleProvider(options =>
{
    options.ProviderName = providerName;
    options.ApiKey = apiKey;
    options.BaseUrl = baseUrl;
    options.RelativePath = relativePath;
    options.AuthMode = authMode;
    options.ApiKeyHeaderName = customHeaderName;
});

var serviceProvider = services.BuildServiceProvider();
var chatClient = serviceProvider.GetRequiredService<IChatClient>();

var response = await chatClient.CompleteAsync(new ChatRequest
{
    ModelId = $"{providerName}:{model}",
    Messages = [ChatMessage.User("Say hello from an OpenAI-compatible provider in one sentence.")]
});

if (!response.IsSuccess)
{
    Console.WriteLine($"Error: {response.ErrorMessage}");
    return;
}

Console.WriteLine(response.Message?.TextContent);
