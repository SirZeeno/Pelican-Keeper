using Pelican_Keeper.Discord;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Tests;

/// <summary>
/// Tests for live message storage operations.
/// </summary>
public class LiveMessageStorageTests
{
    private readonly ulong _testMessageId = 393093003;

    [SetUp]
    public void Setup()
    {
        Logger.SuppressExitForTests = true;

        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "MessageHistory.json");
        if (!File.Exists(path))
            File.WriteAllText(path, "{}");

        LiveMessageStorage.LoadAll(path);
    }

    [Test]
    public void LoadAll_ReturnsNonNull()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "MessageHistory.json");
        var storage = LiveMessageStorage.LoadAll(path);
        Assert.That(storage, Is.Not.Null);
    }

    [Test]
    public void LoadAll_WithValidPath_LoadsSuccessfully()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "MessageHistory.json");
        File.WriteAllText(path, "{\"LiveStore\":[],\"PaginatedLiveStore\":{}}");

        var storage = LiveMessageStorage.LoadAll(path);
        Assert.That(storage?.LiveStore, Is.Not.Null);
        Assert.That(storage?.PaginatedLiveStore, Is.Not.Null);
    }

    [Test]
    public void SavePaginatedMessage_StoresPageIndex()
    {
        // Only test paginated storage as it has clearer behavior
        LiveMessageStorage.Save(_testMessageId, 5);
        var pageIndex = LiveMessageStorage.GetPaginated(_testMessageId);
        Assert.That(pageIndex, Is.EqualTo(5));
    }
}