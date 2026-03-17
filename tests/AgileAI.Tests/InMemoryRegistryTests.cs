using AgileAI.Abstractions;
using AgileAI.Core;

namespace AgileAI.Tests;

public class InMemoryRegistryTests
{
    [Fact]
    public void InMemoryToolRegistry_RegisterAndGetTool_ShouldWork()
    {
        var registry = new InMemoryToolRegistry();
        var mockTool = new MockTool("test-tool", "test description");

        registry.Register(mockTool);

        Assert.True(registry.TryGetTool("test-tool", out var retrievedTool));
        Assert.NotNull(retrievedTool);
        Assert.Equal("test-tool", retrievedTool.Name);
    }

    [Fact]
    public void InMemoryToolRegistry_GetToolDefinitions_ShouldReturnAllRegisteredTools()
    {
        var registry = new InMemoryToolRegistry();
        registry.Register(new MockTool("tool1", "desc1"));
        registry.Register(new MockTool("tool2", "desc2"));

        var definitions = registry.GetToolDefinitions();

        Assert.Equal(2, definitions.Count);
        Assert.Contains(definitions, d => d.Name == "tool1");
        Assert.Contains(definitions, d => d.Name == "tool2");
    }

    [Fact]
    public void InMemoryToolRegistry_RegisterMultiple_ShouldWork()
    {
        var registry = new InMemoryToolRegistry();
        var tools = new List<ITool>
        {
            new MockTool("tool1", "desc1"),
            new MockTool("tool2", "desc2")
        };

        registry.Register(tools);

        Assert.True(registry.TryGetTool("tool1", out _));
        Assert.True(registry.TryGetTool("tool2", out _));
    }

    [Fact]
    public void InMemorySkillRegistry_RegisterAndGetSkill_ShouldWork()
    {
        var registry = new InMemorySkillRegistry();
        var mockSkill = new MockSkill("test-skill", "test description");

        registry.Register(mockSkill);

        Assert.True(registry.TryGetSkill("test-skill", out var retrievedSkill));
        Assert.NotNull(retrievedSkill);
        Assert.Equal("test-skill", retrievedSkill.Name);
    }

    [Fact]
    public void InMemorySkillRegistry_GetAllSkills_ShouldReturnAllRegisteredSkills()
    {
        var registry = new InMemorySkillRegistry();
        registry.Register(new MockSkill("skill1", "desc1"));
        registry.Register(new MockSkill("skill2", "desc2"));

        var skills = registry.GetAllSkills();

        Assert.Equal(2, skills.Count);
        Assert.Contains(skills, s => s.Name == "skill1");
        Assert.Contains(skills, s => s.Name == "skill2");
    }

    private class MockTool : ITool
    {
        public string Name { get; }
        public string? Description { get; }
        public object? ParametersSchema { get; }

        public MockTool(string name, string? description)
        {
            Name = name;
            Description = description;
        }

        public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ToolResult { ToolCallId = context.ToolCall.Id, Content = "test" });
        }
    }

    private class MockSkill : ISkill
    {
        public string Name { get; }
        public string? Description { get; }

        public MockSkill(string name, string? description)
        {
            Name = name;
            Description = description;
        }

        public Task<AgentResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AgentResult { IsSuccess = true });
        }
    }
}
