using Pelican_Keeper.Configuration;
using Pelican_Keeper.Discord;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Tests;

/// <summary>
/// Tests for configuration file reading operations.
/// </summary>
public class FileReadingTests
{
    [SetUp]
    public void Setup()
    {
        Logger.SuppressExitForTests = true;
    }

    [Test]
    public async Task ReadConfigFile()
    {
        var configPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Config.json");

        if (!File.Exists(configPath))
        {
            Assert.Inconclusive("Config.json not found in test directory.");
            return;
        }

        var config = await FileManager.ReadConfigFileAsync(configPath);
        Assert.That(config, Is.Not.Null, "Config file should be readable.");
    }

    [Test]
    public async Task ReadGamesToMonitorFile()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "GamesToMonitor.json");

        if (!File.Exists(path))
        {
            Assert.Inconclusive("GamesToMonitor.json not found in test directory.");
            return;
        }

        var games = await FileManager.ReadGamesToMonitorFileAsync(path);
        Assert.That(games, Is.Not.Null, "GamesToMonitor file should be readable.");
        Assert.That(games!.Count, Is.GreaterThan(0), "Should contain at least one game mapping.");
    }

    [Test]
    public void ReadMessageHistoryFile()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "MessageHistory.json");

        // Create a temp message history file for testing
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "{}");
        }

        var storage = LiveMessageStorage.LoadAll(path);
        Assert.That(storage, Is.Not.Null, "Message history should be readable.");
    }
}
