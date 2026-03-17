using System.Net;
using Microsoft.Extensions.Logging;

namespace AgileAI.Providers.AzureOpenAI;

public class AzureOpenAIRetryHttpMessageHandler : DelegatingHandler
{
    private readonly AzureOpenAIOptions _options;
    private readonly ILogger<AzureOpenAIRetryHttpMessageHandler>? _logger;

    public AzureOpenAIRetryHttpMessageHandler(AzureOpenAIOptions options, ILogger<AzureOpenAIRetryHttpMessageHandler>? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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

                if (IsTransientFailure(response.StatusCode) && retryAttempt < _options.MaxRetryCount)
                {
                    retryAttempt++;
                    var delay = CalculateDelay(retryAttempt);
                    _logger?.LogWarning("Azure OpenAI request failed with status code {StatusCode}. Retrying attempt {RetryAttempt}/{MaxRetryCount} after {Delay}ms",
                        response.StatusCode, retryAttempt, _options.MaxRetryCount, delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return response;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (retryAttempt < _options.MaxRetryCount)
                {
                    retryAttempt++;
                    var delay = CalculateDelay(retryAttempt);
                    _logger?.LogWarning("Azure OpenAI request timed out. Retrying attempt {RetryAttempt}/{MaxRetryCount} after {Delay}ms",
                        retryAttempt, _options.MaxRetryCount, delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                _logger?.LogError("Azure OpenAI request timed out after {MaxRetryCount} retries", _options.MaxRetryCount);
                throw;
            }
            catch (HttpRequestException ex)
            {
                if (retryAttempt < _options.MaxRetryCount)
                {
                    retryAttempt++;
                    var delay = CalculateDelay(retryAttempt);
                    _logger?.LogWarning(ex, "Azure OpenAI network error. Retrying attempt {RetryAttempt}/{MaxRetryCount} after {Delay}ms",
                        retryAttempt, _options.MaxRetryCount, delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                _logger?.LogError(ex, "Azure OpenAI network error after {MaxRetryCount} retries", _options.MaxRetryCount);
                throw;
            }
        }
    }

    private static bool IsTransientFailure(HttpStatusCode statusCode)
        => statusCode == HttpStatusCode.TooManyRequests || ((int)statusCode >= 500 && (int)statusCode < 600);

    private TimeSpan CalculateDelay(int retryAttempt)
        => TimeSpan.FromMilliseconds(_options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1));
}
