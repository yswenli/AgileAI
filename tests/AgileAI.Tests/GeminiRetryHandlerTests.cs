using System.Net;
using AgileAI.Providers.Gemini;

namespace AgileAI.Tests;

public class GeminiRetryHandlerTests
{
    [Fact]
    public async Task GeminiRetryHttpMessageHandler_With500Then200_ShouldSucceedOnRetry()
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

        var options = new GeminiOptions { ApiKey = "test-key", MaxRetryCount = 3, InitialRetryDelay = TimeSpan.FromMilliseconds(10) };
        var retryHandler = new GeminiRetryHttpMessageHandler(options)
        {
            InnerHandler = fakeHandler
        };
        var httpClient = new HttpClient(retryHandler);

        var result = await httpClient.GetAsync("https://example.com");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, requestCount);
    }
}
