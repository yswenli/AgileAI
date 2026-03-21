using AgileAI.Abstractions;
using AgileAI.Core;

namespace AgileAI.Tests;

public class InMemorySessionStoreTests
{
    [Fact]
    public async Task SaveGetDeleteAsync_ShouldManageConversationState()
    {
        var store = new InMemorySessionStore();
        var state = new ConversationState
        {
            SessionId = "s1",
            History = [ChatMessage.User("hi")],
            ActiveSkill = "weather",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await store.SaveAsync(state);

        var loaded = await store.GetAsync("s1");
        Assert.NotNull(loaded);
        Assert.Equal("weather", loaded!.ActiveSkill);
        Assert.Single(loaded.History);

        await store.DeleteAsync("s1");
        Assert.Null(await store.GetAsync("s1"));
    }
}
