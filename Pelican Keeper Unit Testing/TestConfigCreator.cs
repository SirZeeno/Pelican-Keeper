using System.Text.Json;
using Pelican_Keeper;

namespace Pelican_Keeper_Unit_Testing;

public static class TestConfigCreator
{
    public static TemplateClasses.Config CreateDefaultConfigInstance()
    {
        return new TemplateClasses.Config
        {
            InternalIpStructure = "192.168.*.*",
            MessageFormat = TemplateClasses.MessageFormat.Consolidated,
            MessageSorting = TemplateClasses.MessageSorting.Name,
            MessageSortingDirection = TemplateClasses.MessageSortingDirection.Ascending,
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
            AutoUpdate = true
        };
    }

    public static List<TemplateClasses.GamesToMonitor>? PullGamesToMonitor()
    {
        return JsonSerializer.Deserialize<List<TemplateClasses.GamesToMonitor>>(HelperClass.GetJsonTextAsync("https://raw.githubusercontent.com/SirZeeno/Pelican-Keeper/refs/heads/testing/Pelican%20Keeper/GamesToMonitor.json").GetAwaiter().GetResult());
    }
}