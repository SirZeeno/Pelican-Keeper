using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pelican_Keeper.Query_Services;
using RestSharp;

namespace Pelican_Keeper;

using static TemplateClasses;
using static HelperClass;

public static class PelicanInterface
{
    private static List<GamesToMonitor>? _gamesToMonitor = FileManager.ReadGamesToMonitorFile().GetAwaiter().GetResult();
    private static List<EggInfo>? _eggsList;
    private static List<RconService> _rconServices = new();
    private static Dictionary<string, DateTime> _shutdownTracker = new();

    private static void GetEggList()
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/application/eggs");
        var response = CreateRequest(client, Program.Secrets.ClientToken);
        try { if (!string.IsNullOrWhiteSpace(response.Content)) { _eggsList = JsonHandler.ExtractEggInfo(response.Content); return; } ConsoleExt.WriteLineWithStepPretext("Empty Egg List response content.", ConsoleExt.CurrentStep.PelicanApiRequest); }
        catch (JsonException ex) { ConsoleExt.WriteLineWithStepPretext("JSON deserialization or fetching Error: " + ex.Message, ConsoleExt.CurrentStep.PelicanApiRequest); }
    }

    public static void GetServerStats(ServerInfo serverInfo)
    {
        if (string.IsNullOrWhiteSpace(serverInfo.Uuid)) { ConsoleExt.WriteLineWithStepPretext("UUID is null or empty.", ConsoleExt.CurrentStep.PelicanApiRequest, ConsoleExt.OutputType.Error); return; }
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/client/servers/" + serverInfo.Uuid + "/resources");
        var response = CreateRequest(client, Program.Secrets.ClientToken);
        try { if (!string.IsNullOrWhiteSpace(response.Content)) { var stats = JsonHandler.ExtractServerResources(response.Content); serverInfo.Resources = stats; return; } ConsoleExt.WriteLineWithStepPretext("Empty Stats response content.", ConsoleExt.CurrentStep.PelicanApiRequest); }
        catch (JsonException ex) { ConsoleExt.WriteLineWithStepPretext("JSON deserialization or fetching Error: " + ex.Message, ConsoleExt.CurrentStep.PelicanApiRequest); }
    }

    private static void GetServerAllocations(List<ServerInfo> serverInfos)
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/client/?type=admin-all");
        var response = CreateRequest(client, Program.Secrets.ClientToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var allocations = JsonHandler.ExtractNetworkAllocations(response.Content);
                foreach (var serverInfo in serverInfos) { serverInfo.Allocations = allocations.Where(s => s.Uuid == serverInfo.Uuid).ToList(); }
                if (!Program.Config.PlayerCountDisplay) return;
                foreach (var serverInfo in serverInfos)
                {
                    bool isTracked = _shutdownTracker.Any(x => x.Key == serverInfo.Uuid);
                    if (serverInfo.Resources?.CurrentState.ToLower() != "offline" && serverInfo.Resources?.CurrentState.ToLower() != "stopping" && serverInfo.Resources?.CurrentState.ToLower() != "starting" && serverInfo.Resources?.CurrentState.ToLower() != "missing")
                    {
                        if (!isTracked) { _shutdownTracker[serverInfo.Uuid] = DateTime.Now; ConsoleExt.WriteLineWithStepPretext($"{serverInfo.Name} is tracked for shutdown: {isTracked}", ConsoleExt.CurrentStep.PelicanApiRequest); }
                        MonitorServers(serverInfo, response.Content);
                        if (Program.Config.AutomaticShutdown)
                        {
                            if (serverInfo.PlayerCountText != "N/A" && !string.IsNullOrEmpty(serverInfo.PlayerCountText))
                            {
                                if (Program.Config.ServersToAutoShutdown != null && Program.Config.ServersToAutoShutdown[0] != "UUIDS HERE" && !Program.Config.ServersToAutoShutdown.Contains(serverInfo.Uuid)) continue;
                                if (_gamesToMonitor == null || _gamesToMonitor.Count == 0) continue;
                                int playerCount = ExtractPlayerCount(serverInfo.PlayerCountText);
                                if (playerCount > 0) { _shutdownTracker[serverInfo.Uuid] = DateTime.Now; }
                                else
                                {
                                    TimeSpan.TryParseExact(Program.Config.EmptyServerTimeout, @"d\:hh\:mm", CultureInfo.InvariantCulture, out var timeTillShutdown);
                                    if (timeTillShutdown == TimeSpan.Zero) timeTillShutdown = TimeSpan.FromHours(1);
                                    if (DateTime.Now - _shutdownTracker[serverInfo.Uuid] >= timeTillShutdown) { SendPowerCommand(serverInfo.Uuid, "stop"); _shutdownTracker.Remove(serverInfo.Uuid); }
                                }
                            }
                        }
                    }
                    else if (isTracked) { _shutdownTracker.Remove(serverInfo.Uuid); }
                }
                return;
            }
            ConsoleExt.WriteLineWithStepPretext("Empty Allocations response content.", ConsoleExt.CurrentStep.PelicanApiRequest);
        }
        catch (JsonException ex) { ConsoleExt.WriteLineWithStepPretext("JSON deserialization error: " + ex.Message, ConsoleExt.CurrentStep.PelicanApiRequest, ConsoleExt.OutputType.Error, ex); }
    }

    internal static List<ServerInfo> GetServersList()
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/application/servers");
        var response = CreateRequest(client, Program.Secrets.ServerToken);
        try
        {
            if (response.Content != null)
            {
                var servers = JsonHandler.ExtractServerListInfo(response.Content);
                GetEggList();
                foreach (var serverInfo in servers) { var foundEgg = _eggsList?.Find(x => x.Id == serverInfo.Egg.Id); if (foundEgg != null) serverInfo.Egg.Name = foundEgg.Name; }
                _ = GetServerStatsList(servers);
                if (Program.Config.ServersToIgnore != null && Program.Config.ServersToIgnore.Length > 0 && Program.Config.ServersToIgnore[0] != "UUIDS HERE") servers = servers.Where(s => !Program.Config.ServersToIgnore.Contains(s.Uuid)).ToList();
                if (Program.Config.IgnoreOfflineServers) servers = servers.Where(s => s.Resources?.CurrentState.ToLower() != "offline" && s.Resources?.CurrentState.ToLower() != "missing").ToList();
                if (Program.Config.IgnoreInternalServers && Program.Config.InternalIpStructure != null)
                {
                    string internalIpPattern = "^" + Regex.Escape(Program.Config.InternalIpStructure).Replace("\\*", "\\d+") + "$";
                    servers = servers.Where(s => s.Allocations != null && s.Allocations.Any(a => !Regex.IsMatch(a.Ip, internalIpPattern))).ToList();
                }
                if (Program.Config.LimitServerCount && Program.Config.MaxServerCount > 0)
                {
                    if (Program.Config.ServersToDisplay != null && Program.Config.ServersToDisplay.Length > 0 && Program.Config.ServersToDisplay[0] != "UUIDS HERE") servers = servers.Where(s => Program.Config.ServersToDisplay.Contains(s.Uuid)).ToList();
                    else servers = servers.Take(Program.Config.MaxServerCount).ToList();
                }
                return SortServers(servers, Program.Config.MessageSorting, Program.Config.MessageSortingDirection);
            }
            ConsoleExt.WriteLineWithStepPretext("Empty Server List response.", ConsoleExt.CurrentStep.PelicanApiRequest, ConsoleExt.OutputType.Error);
        }
        catch (JsonException ex) { ConsoleExt.WriteLineWithStepPretext("JSON error: " + ex.Message, ConsoleExt.CurrentStep.PelicanApiRequest, ConsoleExt.OutputType.Error, ex); }
        return [];
    }

    private static async Task GetServerStatsList(List<ServerInfo> servers)
    {
        var sem = new SemaphoreSlim(5);
        var statsTasks = servers.Select(async server => { await sem.WaitAsync(); try { GetServerStats(server); } finally { sem.Release(); } });
        await Task.WhenAll(statsTasks);
        GetServerAllocations(servers);
    }

    public static void SendPowerCommand(string? uuid, string command)
    {
        if (string.IsNullOrWhiteSpace(uuid) || string.IsNullOrWhiteSpace(command)) return;
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/client/servers/");
        var request = new RestRequest($"{uuid}/power", Method.Post);
        request.AddHeader("Authorization", $"Bearer {Program.Secrets.ClientToken}");
        request.AddHeader("Content-Type", "application/json");
        request.AddStringBody(JsonSerializer.Serialize(new { signal = $"{command}" }), ContentType.Json);
        client.Execute(request);
    }

    public static async Task<string> SendRconGameServerCommand(string ip, int port, string password, string command, string? regexPattern = null)
    {
        RconService rcon = new RconService(ip, port, password);
        if (_rconServices.Any(x => x.Ip == ip && x.Port == port)) rcon = _rconServices.First(x => x.Ip == ip && x.Port == port);
        await rcon.Connect();
        string response = await rcon.SendCommandAsync(command, regexPattern);
        if (!_rconServices.Contains(rcon)) _rconServices.Add(rcon);
        return response;
    }

    public static async Task<string> SendA2SRequest(string ip, int port)
    {
        A2SService a2S = new A2SService(ip, port);
        await a2S.Connect();
        string response = await a2S.SendCommandAsync();
        a2S.Dispose();
        return response;
    }

    public static async Task<string?> SendBedrockMinecraftRequest(string ip, int port)
    {
        BedrockMinecraftQueryService service = new BedrockMinecraftQueryService(ip, port);
        await service.Connect();
        string response = await service.SendCommandAsync();
        service.Dispose();
        return response;
    }

    public static async Task<string?> SendJavaMinecraftRequest(string ip, int port)
    {
        JavaMinecraftQueryService service = new JavaMinecraftQueryService(ip, port);
        await service.Connect();
        string response = await service.SendCommandAsync();
        service.Dispose();
        return response;
    }

    private static void MonitorServers(ServerInfo serverInfo, string json)
    {
        if (_gamesToMonitor == null || _gamesToMonitor.Count == 0) return;
        var serverToMonitor = _gamesToMonitor.FirstOrDefault(s => s.Game == serverInfo.Egg.Name);
        if (serverToMonitor == null) return;

        int maxPlayers = JsonHandler.ExtractMaxPlayerCount(json, serverInfo.Uuid, serverToMonitor.MaxPlayerVariable, serverToMonitor.MaxPlayer);

        switch (serverToMonitor.Protocol)
        {
            case CommandExecutionMethod.Terraria:
                {
                    int queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable);
                    if (queryPort == 0) queryPort = GetConnectableAllocation(serverInfo)?.Port ?? 0;

                    if (Program.Secrets.ExternalServerIp != null && queryPort != 0)
                    {
                        using var terrariaService = new TerrariaQueryService(GetCorrectIp(serverInfo), queryPort);
                        var response = terrariaService.SendCommandAsync().GetAwaiter().GetResult();

                        // FIX: If we get "Online" but no numbers, try to append the Max Players from the Egg Variable
                        if (response == "Online" && maxPlayers > 0)
                        {
                            serverInfo.PlayerCountText = $"?/{maxPlayers}";
                        }
                        else
                        {
                            serverInfo.PlayerCountText = response;
                        }
                    }
                    break;
                }
            case CommandExecutionMethod.A2S:
                {
                    int queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable);
                    if (queryPort != 0 && Program.Secrets.ExternalServerIp != null)
                    {
                        serverInfo.PlayerCountText = SendA2SRequest(GetCorrectIp(serverInfo), queryPort).GetAwaiter().GetResult();
                    }
                    break;
                }
            case CommandExecutionMethod.Rcon:
                {
                    int rconPort = JsonHandler.ExtractRconPort(json, serverInfo.Uuid, serverToMonitor.RconPortVariable);
                    var rconPassword = serverToMonitor.RconPassword ?? JsonHandler.ExtractRconPassword(json, serverInfo.Uuid, serverToMonitor.RconPasswordVariable);
                    if (rconPort != 0 && !string.IsNullOrWhiteSpace(rconPassword) && Program.Secrets.ExternalServerIp != null && serverToMonitor.Command != null)
                    {
                        var regex = _gamesToMonitor.First(s => s.Game == serverInfo.Egg.Name).PlayerCountExtractRegex;
                        var response = SendRconGameServerCommand(GetCorrectIp(serverInfo), rconPort, rconPassword, serverToMonitor.Command, regex).GetAwaiter().GetResult();
                        serverInfo.PlayerCountText = ServerPlayerCountDisplayCleanup(response, maxPlayers);
                    }
                    break;
                }
            case CommandExecutionMethod.MinecraftJava:
                {
                    int queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable);
                    if (queryPort != 0 && Program.Secrets.ExternalServerIp != null)
                    {
                        serverInfo.PlayerCountText = SendJavaMinecraftRequest(GetCorrectIp(serverInfo), queryPort).GetAwaiter().GetResult();
                    }
                    break;
                }
            case CommandExecutionMethod.MinecraftBedrock:
                {
                    int queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable);
                    if (queryPort != 0 && Program.Secrets.ExternalServerIp != null)
                    {
                        serverInfo.PlayerCountText = SendBedrockMinecraftRequest(GetCorrectIp(serverInfo), queryPort).GetAwaiter().GetResult();
                    }
                    break;
                }
        }
    }

    public static void GetGamesToMonitorFileAsync()
    {
        Task.Run(async () => { while (Program.Config.ContinuesGamesToMonitorRead) { _gamesToMonitor = await FileManager.ReadGamesToMonitorFile(); await Task.Delay(TimeSpan.FromSeconds(Program.Config.MarkdownUpdateInterval)); } });
    }
}