using AgileAI.Studio.Api.Data;
using AgileAI.Studio.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgileAI.Studio.Api.Infrastructure;

public static class StudioDbSeeder
{
    public static async Task SeedAsync(StudioDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS \"AgentToolSelections\" (\"AgentDefinitionId\" TEXT NOT NULL CONSTRAINT \"PK_AgentToolSelections\" PRIMARY KEY, \"ToolNamesJson\" TEXT NOT NULL)",
            cancellationToken);

        if (await dbContext.ProviderConnections.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var connection = new ProviderConnection
        {
            Id = Guid.NewGuid(),
            Name = "OpenAI Starter",
            ProviderType = ProviderType.OpenAI,
            ApiKey = "demo-local",
            BaseUrl = "mock://studio/v1/",
            IsEnabled = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var model = new StudioModel
        {
            Id = Guid.NewGuid(),
            ProviderConnection = connection,
            DisplayName = "GPT-4o Mini",
            ModelKey = "gpt-4o-mini",
            SupportsStreaming = true,
            SupportsTools = true,
            SupportsVision = true,
            IsEnabled = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var agent = new AgentDefinition
        {
            Id = Guid.NewGuid(),
            StudioModel = model,
            Name = "Studio Concierge",
            Description = "A polished default assistant for product, copy, and ideation.",
            SystemPrompt = "You are AgileAI Studio Concierge. Be clear, practical, and modern. Keep answers concise but useful.",
            Temperature = 0.65d,
            MaxTokens = 1400,
            EnableSkills = false,
            IsPinned = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            AgentDefinition = agent,
            Title = "Welcome chat",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var assistantMessage = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            Conversation = conversation,
            Role = MessageRole.Assistant,
            Content = "Welcome to AgileAI.Studio. Add a real API key in Models, then create your own agents and start chatting.",
            IsStreaming = false,
            CreatedAtUtc = now
        };

        dbContext.ProviderConnections.Add(connection);
        dbContext.Models.Add(model);
        dbContext.Agents.Add(agent);
        dbContext.Conversations.Add(conversation);
        dbContext.Messages.Add(assistantMessage);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
