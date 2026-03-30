using AgileAI.Abstractions;
using AgileAI.Core;

namespace AgileAI.Studio.Api.Services;

public class StudioPromptSkillExecutor(
    IServiceProvider serviceProvider,
    IToolRegistry? toolRegistry = null) : ISkillExecutor
{
    public async Task<AgentResult> ExecuteAsync(
        SkillManifest manifest,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var modelId = context.ModelId ?? context.Request.ModelId ?? throw new InvalidOperationException("ModelId is required");
        var studioModelId = context.Items.TryGetValue("studioModelId", out var rawStudioModelId)
            ? rawStudioModelId?.ToString()
            : null;

        if (!Guid.TryParse(studioModelId, out var parsedStudioModelId))
        {
            throw new InvalidOperationException("studioModelId is required for Studio skill execution.");
        }

        using var scope = serviceProvider.CreateScope();
        var modelCatalogService = scope.ServiceProvider.GetRequiredService<ModelCatalogService>();
        var providerClientFactory = scope.ServiceProvider.GetRequiredService<ProviderClientFactory>();
        var runtimeOptions = await modelCatalogService.GetRuntimeOptionsAsync(parsedStudioModelId, cancellationToken);
        var chatClient = providerClientFactory.CreateClient(runtimeOptions);
        var executor = new PromptSkillExecutor(chatClient, toolRegistry);

        return await executor.ExecuteAsync(manifest, context with { ModelId = modelId }, cancellationToken);
    }
}
