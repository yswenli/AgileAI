namespace AgileAI.Providers.OpenAICompatible;

public class OpenAICompatibleOptions
{
    public string ProviderName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string RelativePath { get; set; } = "chat/completions";
    public OpenAICompatibleAuthMode AuthMode { get; set; } = OpenAICompatibleAuthMode.Bearer;
    public string? ApiKeyHeaderName { get; set; }

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
}
