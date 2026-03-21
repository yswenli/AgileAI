using System.Net;
using Microsoft.Extensions.Logging;

namespace AgileAI.Providers.Gemini;

public class GeminiRetryHttpMessageHandler : DelegatingHandler
{
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiRetryHttpMessageHandler>? _logger;

    public GeminiRetryHttpMessageHandler(GeminiOptions options, ILogger<GeminiRetryHttpMessageHandler>? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        int retryAttempt = 0;

        while (true)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(_options.RequestTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                response = await base.SendAsync(request, linkedCts.Token);

                if (IsTransientFailure(response.StatusCode))
                {
                    if (retryAttempt < _options.MaxRetryCount)
                    {
                        retryAttempt++;
                        var delay = CalculateDelay(retryAttempt);
                        _logger?.LogWarning(
                            "Request failed with status code {StatusCode}. Retrying attempt {RetryAttempt}/{MaxRetryCount} after {Delay}ms",
                            response.StatusCode, retryAttempt, _options.MaxRetryCount, delay.TotalMilliseconds);
                        
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                    _logger?.LogError(
                        "Request failed with status code {StatusCode} after {MaxRetryCount} retries",
                        response.StatusCode, _options.MaxRetryCount);
                }

                return response;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (retryAttempt < _options.MaxRetryCount)
                {
                    retryAttempt++;
                    var delay = CalculateDelay(retryAttempt);
                    _logger?.LogWarning(
                        "Request timed out. Retrying attempt {RetryAttempt}/{MaxRetryCount} after {Delay}ms",
                        retryAttempt, _options.MaxRetryCount, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
                _logger?.LogError("Request timed out after {MaxRetryCount} retries", _options.MaxRetryCount);
                throw;
            }
            catch (HttpRequestException ex)
            {
                if (retryAttempt < _options.MaxRetryCount)
                {
                    retryAttempt++;
                    var delay = CalculateDelay(retryAttempt);
                    _logger?.LogWarning(
                        ex, "Network error occurred. Retrying attempt {RetryAttempt}/{MaxRetryCount} after {Delay}ms",
                        retryAttempt, _options.MaxRetryCount, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
                _logger?.LogError(ex, "Network error occurred after {MaxRetryCount} retries", _options.MaxRetryCount);
                throw;
            }
        }
    }

    private static bool IsTransientFailure(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests ||
               (int)statusCode >= 500 && (int)statusCode < 600;
    }

    private TimeSpan CalculateDelay(int retryAttempt)
    {
        var delay = TimeSpan.FromMilliseconds(
            _options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1));
        return delay;
    }
}
