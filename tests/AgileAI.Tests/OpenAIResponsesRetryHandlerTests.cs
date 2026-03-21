using System.Net;
using AgileAI.Providers.OpenAIResponses;

namespace AgileAI.Tests;

public class OpenAIResponsesRetryHandlerTests
{
    [Fact]
    public async Task OpenAIResponsesRetryHttpMessageHandler_With500Then200_ShouldSucceedOnRetry()
    {
        var requestCount = 0;
        var fakeHandler = new FakeHttpMessageHandler((request, ct) =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            return Task.FromResult(response);
        });

        var options = new OpenAIResponsesOptions { ApiKey = "test-key", MaxRetryCount = 3, InitialRetryDelay = TimeSpan.FromMilliseconds(10) };
        var retryHandler = new OpenAIResponsesRetryHttpMessageHandler(options)
        {
            InnerHandler = fakeHandler
        };
        var httpClient = new HttpClient(retryHandler);

        var result = await httpClient.GetAsync("https://example.com");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, requestCount);
    }

    [Fact]
    public async Task OpenAIResponsesRetryHttpMessageHandler_WithMaxRetriesReached_ShouldReturnLastFailure()
    {
        var requestCount = 0;
        var fakeHandler = new FakeHttpMessageHandler((request, ct) =>
        {
            requestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });

        var options = new OpenAIResponsesOptions { ApiKey = "test-key", MaxRetryCount = 2, InitialRetryDelay = TimeSpan.FromMilliseconds(10) };
        var retryHandler = new OpenAIResponsesRetryHttpMessageHandler(options)
        {
            InnerHandler = fakeHandler
        };
        var httpClient = new HttpClient(retryHandler);

        var result = await httpClient.GetAsync("https://example.com");

        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal(3, requestCount);
    }

    [Fact]
    public async Task OpenAIResponsesRetryHttpMessageHandler_With400BadRequest_ShouldNotRetry()
    {
        var requestCount = 0;
        var fakeHandler = new FakeHttpMessageHandler((request, ct) =>
        {
            requestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        });

        var options = new OpenAIResponsesOptions { ApiKey = "test-key", MaxRetryCount = 3, InitialRetryDelay = TimeSpan.FromMilliseconds(10) };
        var retryHandler = new OpenAIResponsesRetryHttpMessageHandler(options)
        {
            InnerHandler = fakeHandler
        };
        var httpClient = new HttpClient(retryHandler);

        var result = await httpClient.GetAsync("https://example.com");

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(1, requestCount);
    }
}
