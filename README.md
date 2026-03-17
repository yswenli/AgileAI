# AgileAI

A lightweight .NET AI SDK for building chat applications with provider routing, streaming responses, tool calling, and session management.

> Current status: MVP. The project already includes a working OpenAI provider, tool execution loop, streaming support, samples, and tests.

## Features

- Provider-based chat abstraction
- OpenAI Chat Completions support
  - non-streaming responses
  - streaming responses
  - tool calling
- Chat session management with conversation history
- In-memory tool registry and tool execution loop
- Dependency injection support
- Test coverage for request/response mapping and streaming edge cases

## Project Structure

```text
AgileAI.slnx
├── src/
│   ├── AgileAI.Abstractions/         # Core contracts and models
│   ├── AgileAI.Core/                 # Chat client, session, registries
│   └── AgileAI.Providers.OpenAI/     # OpenAI provider implementation
├── samples/
│   ├── ConsoleChat/                  # Minimal chat sample
│   └── ToolCallingSample/            # Tool calling sample
└── tests/
    └── AgileAI.Tests/                # Unit tests
```

## Architecture Overview

### 1. Abstractions
`AgileAI.Abstractions` defines the core interfaces and models:

- `IChatClient`
- `IChatModelProvider`
- `IChatSession`
- `ITool` / `IToolRegistry`
- `ChatRequest`, `ChatResponse`, `ChatMessage`
- streaming update models such as `TextDeltaUpdate`, `ToolCallDeltaUpdate`, `CompletedUpdate`, `UsageUpdate`

### 2. Core
`AgileAI.Core` provides:

- `ChatClient` for provider routing
- `ChatSession` for multi-turn chat and tool loop handling
- in-memory registries for tools and skills
- dependency injection helpers

### 3. Provider
`AgileAI.Providers.OpenAI` currently implements OpenAI chat completion support with:

- request mapping
- response mapping
- streaming SSE parsing
- tool call delta handling
- retry support for transient failures

## Quick Start

### Direct Usage

```csharp
using AgileAI.Abstractions;
using AgileAI.Core;
using AgileAI.Providers.OpenAI;

var httpClient = new HttpClient();
var provider = new OpenAIChatModelProvider(
    httpClient,
    new OpenAIOptions { ApiKey = "your-api-key" });

var chatClient = new ChatClient();
chatClient.RegisterProvider(provider);

var response = await chatClient.CompleteAsync(new ChatRequest
{
    ModelId = "openai:gpt-4o",
    Messages = [ChatMessage.User("Hello")]
});

Console.WriteLine(response.Message?.TextContent);
```

### Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using AgileAI.Abstractions;
using AgileAI.DependencyInjection;
using AgileAI.Providers.OpenAI.DependencyInjection;

var services = new ServiceCollection();

services.AddAgileAI();
services.AddOpenAIProvider(options =>
{
    options.ApiKey = "your-api-key";
});

var serviceProvider = services.BuildServiceProvider();
var chatClient = serviceProvider.GetRequiredService<IChatClient>();
```

## Streaming Example

```csharp
await foreach (var update in chatClient.StreamAsync(new ChatRequest
{
    ModelId = "openai:gpt-4o",
    Messages = [ChatMessage.User("Tell me a joke")]
}))
{
    switch (update)
    {
        case TextDeltaUpdate text:
            Console.Write(text.Delta);
            break;
        case CompletedUpdate completed:
            Console.WriteLine($"\n[finish_reason={completed.FinishReason}]");
            break;
        case ErrorUpdate error:
            Console.WriteLine($"\nError: {error.ErrorMessage}");
            break;
    }
}
```

## Tool Calling Example

```csharp
var toolRegistry = new InMemoryToolRegistry();
toolRegistry.Register(new WeatherTool());

var session = new ChatSession(chatClient, "openai:gpt-4o", toolRegistry);
var result = await session.SendAsync("What's the weather in San Francisco?");
```

See `samples/ToolCallingSample` for a fuller example.

## OpenAI Options

`OpenAIOptions` supports:

- `ApiKey`
- `BaseUrl`
- `RequestTimeout`
- `MaxRetryCount`
- `InitialRetryDelay`

Retry behavior is intended for transient failures such as:

- HTTP 429
- HTTP 5xx
- network errors
- request timeout

## Streaming Semantics

The current streaming implementation includes some intentional semantics worth noting:

- `ToolCallDeltaUpdate.ToolCallId` is always non-null
- `NameDelta` is only populated when the current delta carries a tool name fragment
- `ArgumentsDelta` is only populated when the current delta carries an arguments fragment
- tool arguments are emitted incrementally, so consumers should accumulate them if they need the final full JSON payload

## Test Status

Recent work added and validated coverage for:

- request mapping
- tool definition mapping
- tool call response mapping
- null / empty `choices`
- null `message`
- usage mapping
- multiple `finish_reason` values
- streaming text / usage / completed updates
- tool call delta accumulation
- invalid streaming JSON lines
- mixed streaming chunks containing text, tool calls, completion, and usage

Current result:

- **49 tests passed**
- **0 failed**

## Build

```bash
dotnet build AgileAI.slnx
```

## Test

```bash
dotnet test AgileAI.slnx
```

## Run Samples

Set your OpenAI API key first:

```bash
export OPENAI_API_KEY="your-api-key"
```

Then run a sample:

```bash
cd samples/ConsoleChat
dotnet run
```

or:

```bash
cd samples/ToolCallingSample
dotnet run
```

## Roadmap Ideas

- additional providers
- richer content parts (image/audio)
- improved skills/runtime support
- package publishing to NuGet
- more advanced tool orchestration and structured outputs

## License

No license has been added yet.
