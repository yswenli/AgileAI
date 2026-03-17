using AgileAI.Abstractions;
using AgileAI.DependencyInjection;
using AgileAI.Providers.AzureOpenAI.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Please set AZURE_OPENAI_ENDPOINT.");
var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Please set AZURE_OPENAI_API_KEY.");
var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
    ?? throw new InvalidOperationException("Please set AZURE_OPENAI_DEPLOYMENT.");
var apiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION") ?? "2024-02-01";

var services = new ServiceCollection();
services.AddAgileAI();
services.AddAzureOpenAIProvider(options =>
{
    options.Endpoint = endpoint;
    options.ApiKey = apiKey;
    options.ApiVersion = apiVersion;
});

var serviceProvider = services.BuildServiceProvider();
var chatClient = serviceProvider.GetRequiredService<IChatClient>();

var response = await chatClient.CompleteAsync(new ChatRequest
{
    ModelId = $"azure-openai:{deployment}",
    Messages = [ChatMessage.User("Say hello from Azure OpenAI in one sentence.")]
});

if (!response.IsSuccess)
{
    Console.WriteLine($"Error: {response.ErrorMessage}");
    return;
}

Console.WriteLine(response.Message?.TextContent);
