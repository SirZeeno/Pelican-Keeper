using Pelican_Keeper.Core;
using Pelican_Keeper.Updates;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Tests;

/// <summary>
/// Tests for version update functionality.
/// </summary>
public class UpdaterTesting
{
    [SetUp]
    public void Setup()
    {
        Logger.SuppressExitForTests = true;
        RuntimeContext.Config = TestConfigCreator.CreateDefaultConfigInstance();
        RuntimeContext.Secrets = TestConfigCreator.CreateDefaultSecretsInstance();
    }

    [TearDown]
    public void TearDown()
    {
        RuntimeContext.Reset();
    }

    [Test]
    public async Task CheckForUpdatesTest()
    {
        Logger.WriteLineWithStep($"Current version: {RuntimeContext.Version}", Logger.Step.Initialization);
        await VersionUpdater.CheckForUpdatesAsync();
        Assert.Pass("Update check completed successfully.");
    }

    [Test]
    public void AutoUpdateEnvironmentVariableTest()
    {
        // Test default behavior (should be false if not set)
        Environment.SetEnvironmentVariable("AUTO_UPDATE", null);
        Assert.That(VersionUpdater.IsAutoUpdateEnabled(), Is.False, "Auto-update should be disabled by default.");

        // Test explicit false
        Environment.SetEnvironmentVariable("AUTO_UPDATE", "false");
        Assert.That(VersionUpdater.IsAutoUpdateEnabled(), Is.False, "Auto-update should be disabled when set to false.");

        // Test explicit true
        Environment.SetEnvironmentVariable("AUTO_UPDATE", "true");
        Assert.That(VersionUpdater.IsAutoUpdateEnabled(), Is.True, "Auto-update should be enabled when set to true.");

        // Test case insensitivity
        Environment.SetEnvironmentVariable("AUTO_UPDATE", "TRUE");
        Assert.That(VersionUpdater.IsAutoUpdateEnabled(), Is.True, "Auto-update should be case-insensitive.");

        // Clean up
        Environment.SetEnvironmentVariable("AUTO_UPDATE", null);
    }
}
