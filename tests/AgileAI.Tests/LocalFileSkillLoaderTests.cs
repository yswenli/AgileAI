using AgileAI.Abstractions;
using AgileAI.Core;
using Moq;

namespace AgileAI.Tests;

public class LocalFileSkillLoaderTests
{
    [Fact]
    public async Task LoadFromDirectoryAsync_ShouldLoadSkillManifestFromSkillMd()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var skillDir = Path.Combine(root, "weather");
        Directory.CreateDirectory(skillDir);
        await File.WriteAllTextAsync(Path.Combine(skillDir, "SKILL.md"), """
---
name: weather
description: Weather helper
version: 1.0.0
entry: prompt
triggers:
  - weather
  - forecast
files:
  - examples.md
---
# Weather Skill
Give a short forecast.
""");

        try
        {
            var loader = new LocalFileSkillLoader();
            var manifests = await loader.LoadFromDirectoryAsync(root);

            var manifest = Assert.Single(manifests);
            Assert.Equal("weather", manifest.Name);
            Assert.Equal("Weather helper", manifest.Description);
            Assert.Equal("1.0.0", manifest.Version);
            Assert.Equal("prompt", manifest.EntryMode);
            Assert.Contains("weather", manifest.Triggers);
            Assert.Contains("forecast", manifest.Triggers);
            Assert.Contains("examples.md", manifest.Files);
            Assert.Contains("Give a short forecast.", manifest.InstructionBody);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
