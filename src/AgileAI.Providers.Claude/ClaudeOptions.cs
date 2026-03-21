namespace AgileAI.Providers.Claude;

public class ClaudeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string Version { get; set; } = "2023-06-01";
    
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
}
