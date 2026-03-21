using Moq;
using AgileAI.Abstractions;
using AgileAI.Core;

namespace AgileAI.Tests;

public class InMemorySkillRegistryTests
{
    [Fact]
    public void InMemorySkillRegistry_RegisterSkill_ShouldStoreSkill()
    {
        var registry = new InMemorySkillRegistry();
        var mockSkill = new Mock<ISkill>();
        mockSkill.Setup(s => s.Name).Returns("test-skill");
        
        registry.Register(mockSkill.Object);
        
        Assert.True(registry.TryGetSkill("test-skill", out var retrievedSkill));
        Assert.Equal("test-skill", retrievedSkill?.Name);
    }

    [Fact]
    public void InMemorySkillRegistry_RegisterMultipleSkills_ShouldStoreAllSkills()
    {
        var registry = new InMemorySkillRegistry();
        var skill1 = new Mock<ISkill>();
        skill1.Setup(s => s.Name).Returns("skill-1");
        var skill2 = new Mock<ISkill>();
        skill2.Setup(s => s.Name).Returns("skill-2");
        
        registry.Register(new[] { skill1.Object, skill2.Object });
        
        var allSkills = registry.GetAllSkills();
        Assert.Equal(2, allSkills.Count);
    }

    [Fact]
    public void InMemorySkillRegistry_GetSkill_NotFound_ShouldReturnFalse()
    {
        var registry = new InMemorySkillRegistry();
        
        Assert.False(registry.TryGetSkill("non-existent", out _));
    }

    [Fact]
    public void InMemorySkillRegistry_GetAllSkills_Empty_ShouldReturnEmptyList()
    {
        var registry = new InMemorySkillRegistry();
        
        var allSkills = registry.GetAllSkills();
        Assert.Empty(allSkills);
    }
}
