namespace AgileAI.Providers.OpenAIResponses;

public class OpenAIResponsesOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
}
