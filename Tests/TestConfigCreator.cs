using System.Text.Json;
using Pelican_Keeper.Core;
using Pelican_Keeper.Models;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Tests;

/// <summary>
/// Creates test configuration instances for unit testing.
/// </summary>
public static class TestConfigCreator
{
    /// <summary>
    /// Creates a default Config instance for testing.
    /// </summary>
    public static Config CreateDefaultConfigInstance()
    {
        return new Config
        {
            InternalIpStructure = "192.168.*.*",
            MessageFormat = MessageFormat.Consolidated,
            MessageSorting = MessageSorting.Name,
            MessageSortingDirection = MessageSortingDirection.Ascending,
            IgnoreOfflineServers = false,
            IgnoreInternalServers = false,
            ServersToIgnore = ["UUIDS HERE"],

            JoinableIpDisplay = true,
            PlayerCountDisplay = true,
            ServersToMonitor = ["UUIDS HERE"],

            AutomaticShutdown = false,
            ServersToAutoShutdown = ["UUIDS HERE"],
            EmptyServerTimeout = "00:01:00",
            AllowUserServerStartup = true,
            AllowServerStartup = ["UUIDS HERE"],
            UsersAllowedToStartServers = ["USERID HERE"],
            AllowUserServerStopping = false,
            AllowServerStopping = ["USERID HERE"],
            UsersAllowedToStopServers = ["USERID HERE"],

            ContinuesMarkdownRead = true,
            ContinuesGamesToMonitorRead = true,
            MarkdownUpdateInterval = 30,
            ServerUpdateInterval = 10,

            LimitServerCount = false,
            MaxServerCount = 10,
            ServersToDisplay = ["UUIDS HERE"],

            Debug = true,
            DryRun = true,
            AutoUpdate = true,
            NotifyOnUpdate = true
        };
    }

    /// <summary>
    /// Creates a default Secrets instance for testing.
    /// </summary>
    public static Secrets CreateDefaultSecretsInstance()
    {
        return new Secrets(
            ClientToken: "test-client-token",
            ServerToken: "test-server-token",
            ServerUrl: "https://panel.example.com",
            BotToken: "test-bot-token",
            ChannelIds: [123456789],
            NotificationChannelId: null,
            ExternalServerIp: "127.0.0.1"
        );
    }

    /// <summary>
    /// Fetches the GamesToMonitor configuration from the repository.
    /// Requires REPO_OWNER and REPO_NAME environment variables to be set.
    /// </summary>
    public static async Task<List<GamesToMonitor>?> PullGamesToMonitorAsync()
    {
        var url = RepoConfig.GetRawContentUrl("templates/GamesToMonitor.json");
        if (url == null) return null;
        var json = await HttpHelper.GetStringAsync(url);
        return JsonSerializer.Deserialize<List<GamesToMonitor>>(json);
    }
}
