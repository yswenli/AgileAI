using Microsoft.Extensions.DependencyInjection;
using AgileAI.Abstractions;
using AgileAI.DependencyInjection;
using AgileAI.Providers.OpenAI.DependencyInjection;

var services = new ServiceCollection();

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
    ?? throw new InvalidOperationException("Please set the OPENAI_API_KEY environment variable.");

services.AddAgileAI();
services.AddOpenAIProvider(options =>
{
    options.ApiKey = apiKey;
});

var serviceProvider = services.BuildServiceProvider();
var chatClient = serviceProvider.GetRequiredService<IChatClient>();

var request = new ChatRequest
{
    ModelId = "openai:gpt-3.5-turbo",
    Messages =
    [
        ChatMessage.User("Hello! What's your name?")
    ]
};

Console.WriteLine("Sending chat request...");
var response = await chatClient.CompleteAsync(request);

Console.WriteLine($"Response: {response.Message?.TextContent}");