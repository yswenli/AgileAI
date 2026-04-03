namespace AgileAI.Core;

public sealed class LoggingMiddlewareOptions
{
    public bool LogInputs { get; set; }
    public bool LogToolArguments { get; set; }
    public bool LogToolResults { get; set; }
    public bool IncludeMessageCounts { get; set; } = true;
}
