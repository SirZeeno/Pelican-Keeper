using System.Globalization;
using System.Text.RegularExpressions;
using Pelican_Keeper.Configuration;
using Pelican_Keeper.Core;
using Pelican_Keeper.Models;
using Pelican_Keeper.Query;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Pelican;

/// <summary>
/// Orchestrates server monitoring, player count queries, and auto-shutdown logic.
/// </summary>
public static class ServerMonitorService
{
    private static List<GamesToMonitor>? _gamesToMonitor;
    private static List<EggInfo>? _eggsList;
    private static readonly List<RconQueryService> RconConnections = [];
    private static readonly Dictionary<string, DateTime> ShutdownTracker = new();

    /// <summary>
    /// Initializes the games to monitor configuration.
    /// </summary>
    public static async Task InitializeAsync()
    {
        _gamesToMonitor = await FileManager.ReadGamesToMonitorFileAsync();
    }

    /// <summary>
    /// Fetches and processes the complete server list with stats, allocations, and player counts.
    /// </summary>
    public static List<ServerInfo> GetProcessedServerList()
    {
        var servers = PelicanApiClient.GetServerList();
        if (servers.Count == 0)
        {
            Logger.WriteLineWithStep("No servers found on Pelican.", Logger.Step.PelicanApi, Logger.OutputType.Error);
            return [];
        }

        _eggsList ??= PelicanApiClient.GetEggList();

        foreach (var server in servers)
        {
            var egg = _eggsList.Find(e => e.Id == server.Egg.Id);
            if (egg != null) server.Egg.Name = egg.Name;
        }

        // Apply early filters (UUID-based, before fetching stats)
        servers = ApplyEarlyFilters(servers);

        FetchServerStats(servers);
        ProcessAllocationsAndPlayerCounts(servers);

        // Apply late filters (require stats/allocations)
        return ApplyLateFilters(servers);
    }

    private static void FetchServerStats(List<ServerInfo> servers)
    {
        var semaphore = new SemaphoreSlim(5);
        var tasks = servers.Select(async server =>
        {
            await semaphore.WaitAsync();
            try
            {
                PelicanApiClient.GetServerStats(server);
            }
            finally
            {
                semaphore.Release();
            }
        });

        Task.WhenAll(tasks).GetAwaiter().GetResult();
    }

    private static void ProcessAllocationsAndPlayerCounts(List<ServerInfo> servers)
    {
        var allocationsJson = PelicanApiClient.GetServerAllocationsJson();
        if (string.IsNullOrWhiteSpace(allocationsJson))
        {
            Logger.WriteLineWithStep("Empty allocations response.", Logger.Step.PelicanApi);
            return;
        }

        var allocations = JsonResponseParser.ExtractNetworkAllocations(allocationsJson);
        foreach (var server in servers)
            server.Allocations = allocations.Where(a => a.Uuid == server.Uuid).ToList();

        if (!RuntimeContext.Config.PlayerCountDisplay) return;

        foreach (var server in servers)
        {
            var state = server.Resources?.CurrentState.ToLower();
            var isRunning = state != "offline" && state != "stopping" && state != "starting" && state != "missing";

            if (isRunning)
            {
                if (!ShutdownTracker.ContainsKey(server.Uuid))
                {
                    ShutdownTracker[server.Uuid] = DateTime.Now;
                    Logger.WriteLineWithStep($"{server.Name} is now being tracked for shutdown.", Logger.Step.GameMonitoring);
                }

                QueryPlayerCount(server, allocationsJson);
                ProcessAutoShutdown(server);
            }
            else
            {
                ShutdownTracker.Remove(server.Uuid);
            }
        }
    }

    private static void QueryPlayerCount(ServerInfo server, string json)
    {
        if (_gamesToMonitor == null || _gamesToMonitor.Count == 0) return;

        var gameConfig = _gamesToMonitor.FirstOrDefault(g => g.Game == server.Egg.Name);
        if (gameConfig == null) return;

        var maxPlayers = JsonResponseParser.ExtractMaxPlayerCount(json, server.Uuid, gameConfig.MaxPlayerVariable, gameConfig.MaxPlayer);
        var ip = NetworkHelper.GetDisplayIp(server);

        switch (gameConfig.Protocol)
        {
            case CommandExecutionMethod.Terraria:
                QueryTerrariaServer(server, json, gameConfig, ip, maxPlayers);
                break;
            case CommandExecutionMethod.A2S:
                QueryA2SServer(server, json, gameConfig, ip);
                break;
            case CommandExecutionMethod.Rcon:
                QueryRconServer(server, json, gameConfig, ip, maxPlayers);
                break;
            case CommandExecutionMethod.MinecraftJava:
                QueryMinecraftJavaServer(server, json, gameConfig, ip);
                break;
            case CommandExecutionMethod.MinecraftBedrock:
                QueryMinecraftBedrockServer(server, json, gameConfig, ip);
                break;
        }
    }

    private static void QueryTerrariaServer(ServerInfo server, string json, GamesToMonitor config, string ip, int maxPlayers)
    {
        var port = JsonResponseParser.ExtractQueryPort(json, server.Uuid, config.QueryPortVariable);
        if (port == 0) port = NetworkHelper.GetDefaultAllocation(server)?.Port ?? 0;

        if (string.IsNullOrEmpty(RuntimeContext.Secrets.ExternalServerIp) || port == 0) return;

        using var service = new TerrariaQueryService(ip, port);
        var response = service.QueryAsync().GetAwaiter().GetResult();

        server.PlayerCountText = response == "Online" && maxPlayers > 0
            ? $"?/{maxPlayers}"
            : response;
    }

    private static void QueryA2SServer(ServerInfo server, string json, GamesToMonitor config, string ip)
    {
        var port = JsonResponseParser.ExtractQueryPort(json, server.Uuid, config.QueryPortVariable);
        if (port == 0 || string.IsNullOrEmpty(RuntimeContext.Secrets.ExternalServerIp)) return;

        server.PlayerCountText = A2SQueryService.QueryAsync(ip, port).GetAwaiter().GetResult();
    }

    private static void QueryRconServer(ServerInfo server, string json, GamesToMonitor config, string ip, int maxPlayers)
    {
        var port = JsonResponseParser.ExtractRconPort(json, server.Uuid, config.RconPortVariable);
        var password = config.RconPassword ?? JsonResponseParser.ExtractRconPassword(json, server.Uuid, config.RconPasswordVariable);

        if (port == 0 || string.IsNullOrWhiteSpace(password) || config.Command == null) return;

        var existing = RconConnections.FirstOrDefault(r => r.Ip == ip && r.Port == port);
        var rcon = existing ?? new RconQueryService(ip, port, password);

        rcon.ConnectAsync().GetAwaiter().GetResult();
        var response = rcon.QueryAsync(config.Command, config.PlayerCountExtractRegex).GetAwaiter().GetResult();

        if (!RconConnections.Contains(rcon)) RconConnections.Add(rcon);

        server.PlayerCountText = PlayerCountHelper.FormatPlayerCount(response, maxPlayers);
    }

    private static void QueryMinecraftJavaServer(ServerInfo server, string json, GamesToMonitor config, string ip)
    {
        var port = JsonResponseParser.ExtractQueryPort(json, server.Uuid, config.QueryPortVariable);
        if (port == 0 || string.IsNullOrEmpty(RuntimeContext.Secrets.ExternalServerIp)) return;

        using var service = new MinecraftJavaQueryService(ip, port);
        service.ConnectAsync().GetAwaiter().GetResult();
        server.PlayerCountText = service.QueryAsync().GetAwaiter().GetResult();
    }

    private static void QueryMinecraftBedrockServer(ServerInfo server, string json, GamesToMonitor config, string ip)
    {
        var port = JsonResponseParser.ExtractQueryPort(json, server.Uuid, config.QueryPortVariable);
        if (port == 0 || string.IsNullOrEmpty(RuntimeContext.Secrets.ExternalServerIp)) return;

        using var service = new MinecraftBedrockQueryService(ip, port);
        service.ConnectAsync().GetAwaiter().GetResult();
        server.PlayerCountText = service.QueryAsync().GetAwaiter().GetResult();
    }

    private static void ProcessAutoShutdown(ServerInfo server)
    {
        if (!RuntimeContext.Config.AutomaticShutdown) return;
        if (string.IsNullOrEmpty(server.PlayerCountText) || server.PlayerCountText == "N/A") return;

        var shutdownList = RuntimeContext.Config.ServersToAutoShutdown;
        if (shutdownList != null && shutdownList.Length > 0 && shutdownList[0] != "UUIDS HERE" && !shutdownList.Contains(server.Uuid))
            return;

        var playerCount = PlayerCountHelper.ExtractPlayerCount(server.PlayerCountText);

        if (playerCount > 0)
        {
            ShutdownTracker[server.Uuid] = DateTime.Now;
            return;
        }

        var timeoutConfig = RuntimeContext.Config.EmptyServerTimeout;
        var parsedTimeout = TimeSpan.TryParseExact(timeoutConfig, @"d\:hh\:mm", CultureInfo.InvariantCulture, out var timeout);
        if (!parsedTimeout)
        {
            timeout = TimeSpan.FromHours(1);
            Logger.WriteLineWithStep(
                $"Invalid EmptyServerTimeout value '{timeoutConfig}' in configuration. Expected format 'd:hh:mm'. Falling back to {timeout}.",
                Logger.Step.GameMonitoring);
        }
        else if (timeout == TimeSpan.Zero)
        {
            timeout = TimeSpan.FromHours(1);
        }

        if (DateTime.Now - ShutdownTracker[server.Uuid] >= timeout)
        {
            Logger.WriteLineWithStep($"Auto-stopping empty server: {server.Name}", Logger.Step.GameMonitoring);
            PelicanApiClient.SendPowerCommand(server.Uuid, "stop");
            ShutdownTracker.Remove(server.Uuid);
        }
    }

    /// <summary>
    /// Applies UUID-based filters before fetching stats (ServersToIgnore, ServersToDisplay limit).
    /// </summary>
    private static List<ServerInfo> ApplyEarlyFilters(List<ServerInfo> servers)
    {
        var config = RuntimeContext.Config;

        if (config.ServersToIgnore?.Length > 0)
            servers = servers.Where(s => !config.ServersToIgnore.Contains(s.Uuid)).ToList();

        if (config.LimitServerCount && config.MaxServerCount > 0)
        {
            if (config.ServersToDisplay?.Length > 0)
                servers = servers.Where(s => config.ServersToDisplay.Contains(s.Uuid)).ToList();
            else
                servers = servers.Take(config.MaxServerCount).ToList();
        }

        return servers;
    }

    /// <summary>
    /// Applies filters that require stats/allocations (IgnoreOfflineServers, IgnoreInternalServers, sorting).
    /// </summary>
    private static List<ServerInfo> ApplyLateFilters(List<ServerInfo> servers)
    {
        var config = RuntimeContext.Config;

        if (config.IgnoreOfflineServers)
            servers = servers.Where(s => s.Resources?.CurrentState.ToLower() != "offline" && s.Resources?.CurrentState.ToLower() != "missing").ToList();

        if (config.IgnoreInternalServers && !string.IsNullOrEmpty(config.InternalIpStructure))
        {
            var pattern = "^" + Regex.Escape(config.InternalIpStructure).Replace("\\*", "\\d+") + "$";
            servers = servers.Where(s => s.Allocations?.Any(a => !Regex.IsMatch(a.Ip, pattern)) == true).ToList();
        }

        return CollectionHelper.SortServers(servers, config.MessageSorting, config.MessageSortingDirection);
    }

    /// <summary>
    /// Starts background task for continuous GamesToMonitor reloading.
    /// </summary>
    public static void StartContinuousGamesReload()
    {
        Task.Run(async () =>
        {
            while (RuntimeContext.Config.ContinuesGamesToMonitorRead)
            {
                _gamesToMonitor = await FileManager.ReadGamesToMonitorFileAsync();
                await Task.Delay(TimeSpan.FromSeconds(RuntimeContext.Config.MarkdownUpdateInterval));
            }
        });
    }
}
