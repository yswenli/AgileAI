using System.Net;
using System.Text;
using System.Text.Json;
using AgileAI.Abstractions;
using AgileAI.Providers.OpenAIResponses;

namespace AgileAI.Tests;

public class OpenAIResponsesStreamingTests
{
    [Fact]
    public async Task StreamAsync_WithTextDeltas_ShouldYieldTextUpdates()
    {
        var fakeHandler = new FakeHttpMessageHandler((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var streamContent = new StringBuilder();
            
            var event1 = new { type = "response.output_text.delta", delta = "Hello" };
            streamContent.AppendLine($"data: {JsonSerializer.Serialize(event1, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })}");
            
            var event2 = new { type = "response.output_text.delta", delta = " World!" };
            streamContent.AppendLine($"data: {JsonSerializer.Serialize(event2, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })}");
            
            var event3 = new { type = "response.finished" };
            streamContent.AppendLine($"data: {JsonSerializer.Serialize(event3, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })}");

            response.Content = new StringContent(streamContent.ToString(), Encoding.UTF8, "text/event-stream");
            return Task.FromResult(response);
        });

        var options = new OpenAIResponsesOptions { ApiKey = "test-key" };
        var httpClient = new HttpClient(fakeHandler);
        var provider = new OpenAIResponsesChatModelProvider(httpClient, options);

        var chatRequest = new ChatRequest
        {
            ModelId = "gpt-4o",
            Messages = [ChatMessage.User("test")]
        };

        var updates = new List<StreamingChatUpdate>();
        await foreach (var update in provider.StreamAsync(chatRequest))
        {
            updates.Add(update);
        }

        var textUpdates = updates.OfType<TextDeltaUpdate>().ToList();
        Assert.Equal(2, textUpdates.Count);
        Assert.Equal("Hello", textUpdates[0].Delta);
        Assert.Equal(" World!", textUpdates[1].Delta);
    }

    [Fact]
    public async Task StreamAsync_WithInvalidJson_ShouldSkipAndContinue()
    {
        var fakeHandler = new FakeHttpMessageHandler((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var streamContent = new StringBuilder();
            
            streamContent.AppendLine("data: invalid json here");
            var validEvent = new { type = "response.output_text.delta", delta = "Valid content" };
            streamContent.AppendLine($"data: {JsonSerializer.Serialize(validEvent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })}");
            streamContent.AppendLine("data: [DONE]");

            response.Content = new StringContent(streamContent.ToString(), Encoding.UTF8, "text/event-stream");
            return Task.FromResult(response);
        });

        var options = new OpenAIResponsesOptions { ApiKey = "test-key" };
        var httpClient = new HttpClient(fakeHandler);
        var provider = new OpenAIResponsesChatModelProvider(httpClient, options);

        var chatRequest = new ChatRequest
        {
            ModelId = "gpt-4o",
            Messages = [ChatMessage.User("test")]
        };

        var updates = new List<StreamingChatUpdate>();
        await foreach (var update in provider.StreamAsync(chatRequest))
        {
            updates.Add(update);
        }

        var textUpdates = updates.OfType<TextDeltaUpdate>().ToList();
        Assert.Single(textUpdates);
        Assert.Equal("Valid content", textUpdates[0].Delta);
    }
}
