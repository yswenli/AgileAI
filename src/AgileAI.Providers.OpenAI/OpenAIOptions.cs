namespace AgileAI.Providers.OpenAI;

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
}
