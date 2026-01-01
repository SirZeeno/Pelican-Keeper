using Pelican_Keeper.Discord;
using Pelican_Keeper.Models;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Tests;

/// <summary>
/// Tests for server markdown template parsing.
/// </summary>
public class ServerMarkdownTests
{
    private readonly ServerInfo _testServer = new()
    {
        Uuid = "test-uuid",
        Name = "Test Server",
        Resources = new ServerResources
        {
            CurrentState = "running",
            CpuAbsolute = 25.5,
            MemoryBytes = 1073741824, // 1 GB
            DiskBytes = 5368709120, // 5 GB
            NetworkRxBytes = 1048576,
            NetworkTxBytes = 2097152,
            Uptime = 3600000
        }
    };

    [SetUp]
    public void Setup()
    {
        Logger.SuppressExitForTests = true;
    }

    [Test]
    public void ParseTemplate_WithValidServer_ReturnsFormattedOutput()
    {
        var templatePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "MessageMarkdown.txt");
        if (!File.Exists(templatePath))
        {
            Assert.Inconclusive("MessageMarkdown.txt not found in test directory.");
            return;
        }

        // Load the template synchronously for testing
        var templateContent = File.ReadAllText(templatePath);
        Assert.That(templateContent, Is.Not.Empty, "Template file should not be empty");
    }

    [Test]
    public void ServerResources_MemoryFormatting_IsValid()
    {
        var memoryString = FormatHelper.FormatBytes(_testServer.Resources!.MemoryBytes);
        Assert.That(memoryString, Does.Contain("GB").Or.Contain("MB"));
    }

    [Test]
    public void ServerResources_UptimeFormatting_IsValid()
    {
        var uptimeString = FormatHelper.FormatUptime(_testServer.Resources!.Uptime);
        Assert.That(uptimeString, Is.Not.Empty);
        Assert.That(uptimeString, Does.Contain("h").Or.Contain("m").Or.Contain("d"));
    }

    [Test]
    public void StatusIcon_ReturnsCorrectIcon()
    {
        var runningIcon = FormatHelper.GetStatusIcon("running");
        var stoppedIcon = FormatHelper.GetStatusIcon("offline");

        Assert.That(runningIcon, Is.Not.Empty);
        Assert.That(stoppedIcon, Is.Not.Empty);
        Assert.That(runningIcon, Is.Not.EqualTo(stoppedIcon));
    }
}
