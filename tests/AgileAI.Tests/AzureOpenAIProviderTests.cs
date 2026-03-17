using System.Net;
using System.Text;
using System.Text.Json;
using AgileAI.Abstractions;
using AgileAI.Providers.AzureOpenAI;

namespace AgileAI.Tests;

public class AzureOpenAIProviderTests
{
    [Fact]
    public void AzureOpenAIChatModelProvider_ProviderName_ShouldBeAzureOpenAI()
    {
        var provider = new AzureOpenAIChatModelProvider(new HttpClient(), new AzureOpenAIOptions
        {
            ApiKey = "test-key",
            Endpoint = "https://example.openai.azure.com/"
        });

        Assert.Equal("azure-openai", provider.ProviderName);
    }

    [Fact]
    public async Task CompleteAsync_ShouldUseAzureDeploymentUrlAndApiKeyHeader()
    {
        string? capturedUrl = null;
        string? apiKeyHeader = null;
        string? body = null;

        var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            capturedUrl = request.RequestUri?.ToString();
            apiKeyHeader = request.Headers.TryGetValues("api-key", out var values) ? values.Single() : null;
            body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    choices = new[]
                    {
                        new { message = new { role = "assistant", content = "Azure ok" }, finish_reason = "stop" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }), Encoding.UTF8, "application/json")
            };
            return response;
        });

        var provider = new AzureOpenAIChatModelProvider(new HttpClient(handler), new AzureOpenAIOptions
        {
            ApiKey = "azure-test-key",
            Endpoint = "https://example.openai.azure.com",
            ApiVersion = "2024-02-01"
        });

        var result = await provider.CompleteAsync(new ChatRequest
        {
            ModelId = "my-deployment",
            Messages = [ChatMessage.User("Hello")]
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Azure ok", result.Message?.TextContent);
        Assert.Equal("azure-test-key", apiKeyHeader);
        Assert.Equal("https://example.openai.azure.com/openai/deployments/my-deployment/chat/completions?api-version=2024-02-01", capturedUrl);
        Assert.Contains("\"messages\"", body);
        Assert.DoesNotContain("\"model\"", body);
    }

    [Fact]
    public async Task CompleteAsync_WithToolCallResponse_ShouldMapCorrectly()
    {
        var handler = new FakeHttpMessageHandler((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    choices = new[]
                    {
                        new
                        {
                            message = new
                            {
                                role = "assistant",
                                tool_calls = new[]
                                {
                                    new { id = "call_456", type = "function", function = new { name = "lookup_weather", arguments = "{\"city\":\"Seattle\"}" } }
                                }
                            },
                            finish_reason = "tool_calls"
                        }
                    },
                    usage = new { prompt_tokens = 11, completion_tokens = 7, total_tokens = 18 }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }), Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var provider = new AzureOpenAIChatModelProvider(new HttpClient(handler), new AzureOpenAIOptions
        {
            ApiKey = "test-key",
            Endpoint = "https://example.openai.azure.com/"
        });

        var result = await provider.CompleteAsync(new ChatRequest
        {
            ModelId = "my-deployment",
            Messages = [ChatMessage.User("What's the weather?")]
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("tool_calls", result.FinishReason);
        Assert.NotNull(result.Message?.ToolCalls);
        Assert.Single(result.Message.ToolCalls);
        Assert.Equal("call_456", result.Message.ToolCalls[0].Id);
        Assert.Equal("lookup_weather", result.Message.ToolCalls[0].Name);
        Assert.Equal("{\"city\":\"Seattle\"}", result.Message.ToolCalls[0].Arguments);
        Assert.NotNull(result.Usage);
        Assert.Equal(18, result.Usage!.TotalTokens);
    }

    [Fact]
    public async Task CompleteAsync_WithNullChoices_ShouldReturnError()
    {
        var handler = new FakeHttpMessageHandler((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var provider = new AzureOpenAIChatModelProvider(new HttpClient(handler), new AzureOpenAIOptions
        {
            ApiKey = "test-key",
            Endpoint = "https://example.openai.azure.com/"
        });

        var result = await provider.CompleteAsync(new ChatRequest
        {
            ModelId = "my-deployment",
            Messages = [ChatMessage.User("test")]
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid response", result.ErrorMessage);
    }

    [Fact]
    public async Task CompleteAsync_WithOptions_ShouldSerializeAzureRequestWithoutModel()
    {
        string? body = null;

        var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    choices = new[]
                    {
                        new { message = new { role = "assistant", content = "ok" }, finish_reason = "stop" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }), Encoding.UTF8, "application/json")
            };
            return response;
        });

        var provider = new AzureOpenAIChatModelProvider(new HttpClient(handler), new AzureOpenAIOptions
        {
            ApiKey = "test-key",
            Endpoint = "https://example.openai.azure.com/"
        });

        await provider.CompleteAsync(new ChatRequest
        {
            ModelId = "my-deployment",
            Messages = [ChatMessage.User("test")],
            Options = new ChatOptions
            {
                Temperature = 0.2,
                TopP = 0.8,
                MaxTokens = 128,
                StopSequences = ["END"],
                Tools =
                [
                    new ToolDefinition
                    {
                        Name = "get_time",
                        Description = "Get current time",
                        ParametersSchema = new { type = "object" }
                    }
                ]
            }
        });

        Assert.NotNull(body);
        Assert.Contains("\"temperature\":0.2", body);
        Assert.Contains("\"top_p\":0.8", body);
        Assert.Contains("\"max_tokens\":128", body);
        Assert.Contains("\"stop\":[\"END\"]", body);
        Assert.Contains("\"tools\"", body);
        Assert.DoesNotContain("\"model\"", body);
    }

    [Fact]
    public async Task StreamAsync_WithToolCallDeltas_ShouldAccumulateToolCallInfo()
    {
        var handler = new FakeHttpMessageHandler((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var streamContent = new StringBuilder();

            var delta1 = new
            {
                id = "test-1",
                choices = new[]
                {
                    new
                    {
                        delta = new
                        {
                            tool_calls = new[]
                            {
                                new { index = 0, id = "call_123", function = new { name = "get_weather", arguments = "" } }
                            }
                        }
                    }
                }
            };
            streamContent.AppendLine($"data: {JsonSerializer.Serialize(delta1, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })}");

            var delta2 = new
            {
                id = "test-1",
                choices = new[]
                {
                    new
                    {
                        delta = new
                        {
                            content = "Hello",
                            tool_calls = new[]
                            {
                                new { index = 0, function = new { arguments = "{\"location\":\"Seattle\"}" } }
                            }
                        },
                        finish_reason = "tool_calls"
                    }
                },
                usage = new { prompt_tokens = 2, completion_tokens = 3, total_tokens = 5 }
            };
            streamContent.AppendLine($"data: {JsonSerializer.Serialize(delta2, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })}");
            streamContent.AppendLine("data: [DONE]");

            response.Content = new StringContent(streamContent.ToString(), Encoding.UTF8, "text/event-stream");
            return Task.FromResult(response);
        });

        var provider = new AzureOpenAIChatModelProvider(new HttpClient(handler), new AzureOpenAIOptions
        {
            ApiKey = "test-key",
            Endpoint = "https://example.openai.azure.com/"
        });

        var updates = new List<StreamingChatUpdate>();
        await foreach (var update in provider.StreamAsync(new ChatRequest
        {
            ModelId = "my-deployment",
            Messages = [ChatMessage.User("test")]
        }))
        {
            updates.Add(update);
        }

        Assert.Collection(updates,
            update =>
            {
                var tool = Assert.IsType<ToolCallDeltaUpdate>(update);
                Assert.Equal("call_123", tool.ToolCallId);
                Assert.Equal("get_weather", tool.NameDelta);
                Assert.Equal(string.Empty, tool.ArgumentsDelta);
            },
            update => Assert.Equal("Hello", Assert.IsType<TextDeltaUpdate>(update).Delta),
            update =>
            {
                var tool = Assert.IsType<ToolCallDeltaUpdate>(update);
                Assert.Equal("call_123", tool.ToolCallId);
                Assert.Null(tool.NameDelta);
                Assert.Equal("{\"location\":\"Seattle\"}", tool.ArgumentsDelta);
            },
            update => Assert.Equal("tool_calls", Assert.IsType<CompletedUpdate>(update).FinishReason),
            update => Assert.Equal(5, Assert.IsType<UsageUpdate>(update).Usage.TotalTokens));
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
