// 主程序
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using AgileAI.Abstractions;
using AgileAI.Core;
using AgileAI.DependencyInjection;
using AgileAI.Providers.OpenAI.DependencyInjection;

var services = new ServiceCollection();

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
    ?? throw new InvalidOperationException("Please set the OPENAI_API_KEY environment variable.");

// 注册 AgileAI 和 OpenAI 提供商
services.AddAgileAI();
services.AddOpenAIProvider(options =>
{
    options.ApiKey = apiKey;
});

var serviceProvider = services.BuildServiceProvider();
var chatClient = serviceProvider.GetRequiredService<IChatClient>();

// 创建工具注册表并注册工具
var toolRegistry = new InMemoryToolRegistry();
toolRegistry.Register(new CalculatorTool());
toolRegistry.Register(new CurrentTimeTool());
toolRegistry.Register(new EchoTool());

// 创建对话会话
var session = new ChatSession(chatClient, "openai:gpt-3.5-turbo", toolRegistry);

Console.WriteLine("=== AgileAI Tool Calling Sample ===");
Console.WriteLine("Available tools: calculator, get_current_time, echo");
Console.WriteLine("Type 'exit' to quit\n");

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    
    if (string.IsNullOrWhiteSpace(input) || input.ToLowerInvariant() == "exit")
        break;

    Console.WriteLine("Assistant: ");
    var response = await session.SendAsync(input);
    
    if (response.IsSuccess)
    {
        Console.WriteLine(response.Message?.TextContent);
    }
    else
    {
        Console.WriteLine($"Error: {response.ErrorMessage}");
    }
    
    Console.WriteLine();
}

// 1. 定义工具 - 计算器
public class CalculatorTool : ITool
{
    public string Name => "calculator";
    public string? Description => "Perform basic arithmetic calculations";
    public object? ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            operation = new { type = "string", @enum = new[] { "add", "subtract", "multiply", "divide" } },
            left = new { type = "number" },
            right = new { type = "number" }
        },
        required = new[] { "operation", "left", "right" }
    };

    public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var args = JsonSerializer.Deserialize<CalculatorArgs>(context.ToolCall.Arguments);
            if (args == null)
                return Task.FromResult(new ToolResult { ToolCallId = context.ToolCall.Id, Content = "Invalid arguments", IsSuccess = false });

            double result = args.Operation switch
            {
                "add" => args.Left + args.Right,
                "subtract" => args.Left - args.Right,
                "multiply" => args.Left * args.Right,
                "divide" => args.Right != 0 ? args.Left / args.Right : throw new DivideByZeroException(),
                _ => throw new NotSupportedException($"Unknown operation: {args.Operation}")
            };

            return Task.FromResult(new ToolResult
            {
                ToolCallId = context.ToolCall.Id,
                Content = result.ToString(),
                IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult
            {
                ToolCallId = context.ToolCall.Id,
                Content = $"Error: {ex.Message}",
                IsSuccess = false
            });
        }
    }

    private class CalculatorArgs
    {
        public string Operation { get; set; } = string.Empty;
        public double Left { get; set; }
        public double Right { get; set; }
    }
}

// 2. 定义工具 - 获取当前时间
public class CurrentTimeTool : ITool
{
    public string Name => "get_current_time";
    public string? Description => "Get the current date and time";
    public object? ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            timezone = new { type = "string", description = "Optional timezone (e.g., 'UTC', 'America/New_York')" }
        }
    };

    public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var timeString = now.ToString("F");
        
        return Task.FromResult(new ToolResult
        {
            ToolCallId = context.ToolCall.Id,
            Content = timeString,
            IsSuccess = true
        });
    }
}

// 3. 定义工具 - 回显
public class EchoTool : ITool
{
    public string Name => "echo";
    public string? Description => "Echo back the input message";
    public object? ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            message = new { type = "string" }
        },
        required = new[] { "message" }
    };

    public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var args = JsonSerializer.Deserialize<EchoArgs>(context.ToolCall.Arguments);
            return Task.FromResult(new ToolResult
            {
                ToolCallId = context.ToolCall.Id,
                Content = args?.Message ?? "No message provided",
                IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult
            {
                ToolCallId = context.ToolCall.Id,
                Content = $"Error: {ex.Message}",
                IsSuccess = false
            });
        }
    }

    private class EchoArgs
    {
        public string Message { get; set; } = string.Empty;
    }
}
