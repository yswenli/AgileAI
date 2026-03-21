namespace AgileAI.Studio.Api.Domain;

public class ProviderConnection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProviderType ProviderType { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string? Endpoint { get; set; }
    public string? ProviderName { get; set; }
    public string? RelativePath { get; set; }
    public string? ApiKeyHeaderName { get; set; }
    public string? AuthMode { get; set; }
    public string? ApiVersion { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<StudioModel> Models { get; set; } = new List<StudioModel>();
}
