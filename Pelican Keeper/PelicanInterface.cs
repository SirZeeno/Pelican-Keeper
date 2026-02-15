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
    private static readonly List<RconService> RconServices = new();
    private static readonly Dictionary<string, DateTime> ShutdownTracker = new();
    
    private static readonly RestResponse LocalServerListResponse = GetServerList();
    private static readonly List<ServerInfo> ServerListResponse = GetPelicanServerList();

    /// <summary>
    /// Gets the entire List of Eggs from the Pelican API
    /// </summary>
    private static void GetEggList(this  List<ServerInfo> servers)
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/application/eggs");
        var response = CreateRequest(client, Program.Secrets.ClientToken);

        if (!response.IsSuccessStatusCode)
            ConsoleExt.WriteLine("Error: " + response.StatusCode, ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, response.ErrorException, true, true);
        
        try
        {
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var eggsList = JsonHandler.ExtractEggInfo(response.Content);
                foreach (var serverInfo in servers)
                {
                    var foundEgg = eggsList?.Find(x => x.Id == serverInfo.Egg.Id);
                    if (foundEgg == null) continue;
                    serverInfo.Egg.Name = foundEgg.Name;
                    ConsoleExt.WriteLine($"Egg Name found: {serverInfo.Egg.Name}", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
                }
                return;
            }
            
            ConsoleExt.WriteLine("Empty Egg List response content.", ConsoleExt.CurrentStep.PelicanApi);
        }
        catch (JsonException ex)
        {
            ConsoleExt.WriteLine("JSON deserialization or fetching Error: " + ex.Message, ConsoleExt.CurrentStep.PelicanApi);
            ConsoleExt.WriteLine("Response content: " + response.Content, ConsoleExt.CurrentStep.PelicanApi);
        }
    }
    
    /// <summary>
    /// Gets the server resources from the Pelican API
    /// </summary>
    /// <param name="serverInfo">Server Info Class</param>
    /// <returns>The server resources response</returns>
    public static void GetServerResources(ServerInfo serverInfo)
    {
        if (string.IsNullOrWhiteSpace(serverInfo.Uuid))
        {
            ConsoleExt.WriteLine("UUID is null or empty.", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error);
            return;
        }
        
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/client/servers/" + serverInfo.Uuid + "/resources");
        var response = CreateRequest(client, Program.Secrets.ClientToken);

        if (!response.IsSuccessStatusCode)
            ConsoleExt.WriteLine("Error: " + response.StatusCode, ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, response.ErrorException, true, true);
        
        try
        {
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var stats = JsonHandler.ExtractServerResources(response.Content);
                if (serverInfo.Resources != null)
                    serverInfo.Resources = serverInfo.Resources with
                    {
                        CurrentState = stats.CurrentState,
                        MemoryBytes = stats.MemoryBytes,
                        CpuAbsolute = stats.CpuAbsolute,
                        DiskBytes = stats.DiskBytes,
                        NetworkRxBytes = stats.NetworkRxBytes,
                        NetworkTxBytes = stats.NetworkTxBytes,
                        Uptime = stats.Uptime
                    };
                else
                    serverInfo.Resources = stats;
                return;
            }
            
            ConsoleExt.WriteLine("Empty Stats response content.", ConsoleExt.CurrentStep.PelicanApi);
        }
        catch (JsonException ex)
        {
            ConsoleExt.WriteLine("JSON deserialization or fetching Error: " + ex.Message, ConsoleExt.CurrentStep.PelicanApi);
            ConsoleExt.WriteLine("Response content: " + response.Content, ConsoleExt.CurrentStep.PelicanApi);
        }
    }

    //TODO: possible addition of filtering out servers that dont belong to the user doing the request
    private static RestResponse GetServerList()
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/client/?type=admin-all");
        var response = CreateRequest(client, Program.Secrets.ClientToken);
        
        if (!response.IsSuccessStatusCode)
            ConsoleExt.WriteLine("Error: " + response.StatusCode, ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, response.ErrorException, true, true);

        if (!string.IsNullOrEmpty(response.Content)) return response;
        ConsoleExt.WriteLine($"Server List Response is null or empty. Response Content: {response.Content}", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, response.ErrorException, true, true);
        throw new Exception("Server List Response is null or empty.");
    }
    
    /// <summary>
    /// Gets the Client Server List from the Pelican API, and gets  the Network Allocations, and tracks the server for player count and automatic shutdown.
    /// </summary>
    /// <param name="serverInfos">List of ServerInfo</param>
    public static void GetServerAllocations(List<ServerInfo> serverInfos)
    {
        var response = LocalServerListResponse;
        
        try
        {
            var allocations = JsonHandler.ExtractNetworkAllocations(response.Content!);
            foreach (var serverInfo in serverInfos)
            {
                serverInfo.Allocations = allocations.Where(s => s.Uuid == serverInfo.Uuid).ToList();
            }
        }
        catch (JsonException ex)
        {
            ConsoleExt.WriteLine("JSON deserialization or fetching Error: " + ex.Message, ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, ex);
            ConsoleExt.WriteLine("Response content: " + response.Content, ConsoleExt.CurrentStep.PelicanApi);
        }
    }

    private static void MonitorServers(List<ServerInfo> serverInfos)
    {
        var response = LocalServerListResponse;
        
        if (!Program.Config.PlayerCountDisplay) return;
        if (serverInfos.Count == 0)
        {
            ConsoleExt.WriteLine("Servers list is empty.", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error);
        }
                
        // Only process servers that have allocations (skip bots/services without ports)
        var serversToProcess = Program.Config.IgnoreServersWithoutAllocations
            ? serverInfos.Where(s => s.Allocations is { Count: > 0 })
            : serverInfos;
                
        foreach (var serverInfo in serversToProcess)
        {
            bool isTracked = ShutdownTracker.Any(x => x.Key == serverInfo.Uuid);
            string? serverState = serverInfo.Resources?.CurrentState.ToLower();
            if (serverState != "offline" && serverState != "stopping" && serverState != "starting" && serverState != "missing")
            {
                if (!isTracked)
                {
                    ShutdownTracker[serverInfo.Uuid] = DateTime.Now;
                    ConsoleExt.WriteLine($"{serverInfo.Name} is tracked for shutdown: {isTracked}", ConsoleExt.CurrentStep.PelicanApi);
                }
                RequestToMonitoringServers(serverInfo, response.Content!);
                        
                if (Program.Config.AutomaticShutdown)
                {
                    if (serverInfo.PlayerCountText != "N/A" && !string.IsNullOrEmpty(serverInfo.PlayerCountText))
                    {
                        if (Program.Config.ServersToAutoShutdown != null && Program.Config.ServersToAutoShutdown[0] != "UUIDS HERE" && !Program.Config.ServersToAutoShutdown.Contains(serverInfo.Uuid))
                        {
                            ConsoleExt.WriteLine($"Server {serverInfo.Name} is not in the auto-shutdown list. Skipping shutdown check.", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
                            continue;
                        }
                                
                        if (_gamesToMonitor == null || _gamesToMonitor.Count == 0)
                        {
                            ConsoleExt.WriteLine("No game communication configuration found. Skipping shutdown check.", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Warning);
                            continue;
                        }
                        int playerCount = ExtractPlayerCount(serverInfo.PlayerCountText);
                        ConsoleExt.WriteLine($"Player count: {playerCount} for server: {serverInfo.Name}", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
                        if (playerCount > 0)
                        {
                            ShutdownTracker[serverInfo.Uuid] = DateTime.Now;
                        }
                        else
                        {
                            TimeSpan.TryParseExact(Program.Config.EmptyServerTimeout, @"d\:hh\:mm", CultureInfo.InvariantCulture, out var timeTillShutdown);
                            if (timeTillShutdown == TimeSpan.Zero)
                                timeTillShutdown = TimeSpan.FromHours(1);
                            if (DateTime.Now - ShutdownTracker[serverInfo.Uuid] >= timeTillShutdown)
                            {
                                SendPowerCommand(serverInfo.Uuid, "stop");
                                ConsoleExt.WriteLine($"Server {serverInfo.Name} has been empty for over an hour. Sending shutdown command.", ConsoleExt.CurrentStep.PelicanApi);
                                ShutdownTracker.Remove(serverInfo.Uuid);
                                ConsoleExt.WriteLine($"Server {serverInfo.Name} is stopping and removed from shutdown tracker.", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
                            }
                        }
                    }
                }
            }
            else if (isTracked)
            {
                ShutdownTracker.Remove(serverInfo.Uuid);
                ConsoleExt.WriteLine($"Server {serverInfo.Name} is offline or stopping. Removed from shutdown tracker.", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
            }
        }
    }

    /// <summary>
    /// Gets the list of servers from the Pelican API
    /// </summary>
    /// <returns>Server Info list</returns>
    private static List<ServerInfo> GetPelicanServerList()
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/application/servers");
        var response = CreateRequest(client, Program.Secrets.ServerToken);
        
        if (!response.IsSuccessStatusCode)
            ConsoleExt.WriteLine("Error: " + response.StatusCode, ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, response.ErrorException, true, true);

        if (!string.IsNullOrEmpty(response.Content) && !string.IsNullOrWhiteSpace(response.Content))
        {
            try
            {
                return JsonHandler.ExtractServerListInfo(response.Content);
            }
            catch (JsonException ex)
            {
                ConsoleExt.WriteLine("JSON deserialization or fetching Error: " + ex.Message, ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, ex);
                ConsoleExt.WriteLine("JSON: " + response.Content, ConsoleExt.CurrentStep.PelicanApi);
            }
        }

        return [];
    }

    /// <summary>
    /// Processed the Server list with the settings defined in the config
    /// </summary>
    /// <param name="servers">Server Info list</param>
    private static List<ServerInfo> ProcessServerList(List<ServerInfo> servers)
    {
        string[]? serversToIgnore = Program.Config.ServersToIgnore;
        if (serversToIgnore is { Length: > 0 } && serversToIgnore[0] != "UUIDS HERE") 
            servers = servers.Where(s => !serversToIgnore.Contains(s.Uuid)).ToList();

        if (Program.Config.IgnoreOfflineServers) 
            servers = servers.Where(s => s.Resources?.CurrentState.ToLower() != "offline" && s.Resources?.CurrentState.ToLower() != "missing").ToList();
                
        servers = SortServers(servers, Program.Config.MessageSorting, Program.Config.MessageSortingDirection);
        _ = GetServerResourcesList(servers);
                
        if (Program.Config.IgnoreInternalServers && Program.Config.InternalIpStructure != null)
        {
            string internalIpPattern = "^" + Regex.Escape(Program.Config.InternalIpStructure).Replace("\\*", "\\d+") + "$";
            servers = servers.Where(s => !(s.Allocations?.Any(a => Regex.IsMatch(a.Ip, internalIpPattern)) ?? false)).ToList();
        }
                
        if (Program.Config.LimitServerCount && Program.Config.MaxServerCount > 0)
        {
            if (Program.Config.ServersToDisplay != null && Program.Config.ServersToDisplay.Length > 0 && Program.Config.ServersToDisplay[0] != "UUIDS HERE")
                servers = servers.Where(s => Program.Config.ServersToDisplay.Contains(s.Uuid)).ToList();
            else
                servers = servers.Take(Program.Config.MaxServerCount).ToList();
        }
        
        return servers;
    }
    
    /// <summary>
    /// Returns a ServerInfo List which has been processed and been filled with all its information
    /// </summary>
    /// <returns>Server list response</returns>
    public static List<ServerInfo> GetServersList()
    {
        List<ServerInfo> serverInfos = GetPelicanServerList();
        GetEggList(serverInfos);
        serverInfos = ProcessServerList(serverInfos);
        _ = GetServerResourcesList(ServerListResponse);
        GetServerAllocations(serverInfos);
        MonitorServers(serverInfos);
        return serverInfos;
    }
    
    /// <summary>
    /// Gets alist of server resources from the Pelican API
    /// </summary>
    /// <param name="servers">List of Game Server Info</param>
    /// <returns>list of server resources responses</returns>
    public static async Task GetServerResourcesList(List<ServerInfo> servers)
    {
        var sem = new SemaphoreSlim(5);

        // Stats tasks
        var statsTasks = servers.Select(async server =>
        {
            await sem.WaitAsync();
            try
            {
                ConsoleExt.WriteLine("Fetched stats for server: " + server.Name, ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
                GetServerResources(server);
            }
            finally { sem.Release(); }
        });
        
        // Run them all
        await Task.WhenAll(statsTasks);
    }

    /// <summary>
    /// Sends a Power command to the specified Server.
    /// </summary>
    /// <param name="uuid">UUID of the Server</param>
    /// <param name="command">Command to send ("start", "stop", etc.)</param>
    public static void SendPowerCommand(string? uuid, string command)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            ConsoleExt.WriteLine("UUID is null or empty.", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error);
            return;
        }
        
        if (string.IsNullOrWhiteSpace(command))
        {
            ConsoleExt.WriteLine("Command is null or empty.", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error);
            return;
        }
        
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/client/servers/");
        var request = new RestRequest($"{uuid}/power", Method.Post);
        
        request.AddHeader("Authorization", $"Bearer {Program.Secrets.ClientToken}");
        request.AddHeader("Content-Type", "application/json");

        var body = new { signal = $"{command}" };
        request.AddStringBody(JsonSerializer.Serialize(body), ContentType.Json);

        var response = client.Execute(request);
        if (string.IsNullOrEmpty(response.Content))
            ConsoleExt.WriteLine(response.Content, ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
    }

    /// <summary>
    /// Sends a RCON Server command to the Specified IP and Port
    /// </summary>
    /// <param name="ip">IP of the Server</param>
    /// <param name="port">Port of the Server</param>
    /// <param name="password">RCON Password of the Server</param>
    /// <param name="command">Game command to send</param>
    /// <param name="regexPattern">Regex Pattern to use when </param>
    /// <returns>The response to the command that was sent</returns>
    // TODO: Generalize the connection protocol calls so I don't have to have separate methods for RCON and A2S and i can just generalize it with the ISendCommand interface.
    public static async Task<string> SendRconGameServerCommand(string ip, int port, string password, string command, string? regexPattern = null)
    {
        RconService rcon = new RconService(ip, port, password);
        if (RconServices.Any(x => x.Ip == ip && x.Port == port))
        {
            rcon = RconServices.First(x => x.Ip == ip && x.Port == port);
            ConsoleExt.WriteLine("Reusing existing RCON connection to " + ip + ":" + port, ConsoleExt.CurrentStep.RconQuery, ConsoleExt.OutputType.Debug);
        }
        else
        {
            ConsoleExt.WriteLine("Creating new RCON connection to " + ip + ":" + port, ConsoleExt.CurrentStep.RconQuery, ConsoleExt.OutputType.Debug);
        }

        await rcon.Connect();
        
        string response = await rcon.SendCommandAsync(command, regexPattern);
        
        RconServices.Add(rcon);
        return response;
    }

    /// <summary>
    /// Sends a A2S(Steam Query) request to the specified IP and Port
    /// </summary>
    /// <param name="ip">IP of the Server</param>
    /// <param name="port">Port of the Server</param>
    /// <returns>The Response to the command that was sent</returns>
    public static async Task<string> SendA2SRequest(string ip, int port)
    {
        A2SService a2S = new A2SService(ip, port);
        
        await a2S.Connect();
        string response = await a2S.SendCommandAsync();
        a2S.Dispose();
        
        return response;
    }

    /// <summary>
    /// Sends a Bedrock Minecraft request to the specified IP and Port
    /// </summary>
    /// <param name="ip">IP of the Server</param>
    /// <param name="port">Port of the Server</param>
    /// <returns></returns>
    public static async Task<string?> SendBedrockMinecraftRequest(string ip, int port)
    {
        BedrockMinecraftQueryService bedrockMinecraftQuery = new BedrockMinecraftQueryService(ip, port);
        
        await bedrockMinecraftQuery.Connect();
        string response = await bedrockMinecraftQuery.SendCommandAsync();
        bedrockMinecraftQuery.Dispose();

        return response;
    }

    /// <summary>
    /// Sends a Java Minecraft request to the specified IP and Port
    /// </summary>
    /// <param name="ip">IP of the Server</param>
    /// <param name="port">Port of the Server</param>
    /// <returns></returns>
    public static async Task<string?> SendJavaMinecraftRequest(string ip, int port)
    {
        JavaMinecraftQueryService javaMinecraftQuery = new JavaMinecraftQueryService(ip, port);
        
        await javaMinecraftQuery.Connect();
        string response = await javaMinecraftQuery.SendCommandAsync();
        javaMinecraftQuery.Dispose();
        
        return response;
    }

    /// <summary>
    /// Monitors a specified Server and getting the Player count, Max player count, and put that into a neat text
    /// </summary>
    /// <param name="serverInfo">The ServerInfo of the specific server</param>
    /// <param name="json">Input JSON</param>
    private static void RequestToMonitoringServers(ServerInfo serverInfo, string json)
    {
        if (_gamesToMonitor == null || _gamesToMonitor.Count == 0) return;
        
        var serverToMonitor = _gamesToMonitor.FirstOrDefault(s => s.Game == serverInfo.Egg.Name);
        if (serverToMonitor == null)
        {
            ConsoleExt.WriteLine("No monitoring configuration found for server: " + serverInfo.Name, ConsoleExt.CurrentStep.GameMonitoring, ConsoleExt.OutputType.Warning);
            return;
        }
        ConsoleExt.WriteLine($"Found Game to Monitor {serverToMonitor.Game}", ConsoleExt.CurrentStep.GameMonitoring);

        int maxPlayers = JsonHandler.ExtractMaxPlayerCount(json, serverInfo.Uuid, serverToMonitor.MaxPlayerVariable, serverToMonitor.MaxPlayer);
        
        switch (serverToMonitor.Protocol)
        {
            case CommandExecutionMethod.A2S:
            {
                int queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable, serverInfo.Allocations);

                ConsoleExt.WriteLine("Query port for server " + serverInfo.Name + ": " + queryPort, ConsoleExt.CurrentStep.A2SQuery);
                if (queryPort == 0)
                {
                    ConsoleExt.WriteLine("No Query port found for server: " + serverInfo.Name, ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Warning);
                    return;
                }

                if (Program.Secrets.ExternalServerIp == null) return;
                ConsoleExt.WriteLine($"Sending A2S request to {Program.Secrets.ExternalServerIp}:{queryPort} for server {serverInfo.Name}", ConsoleExt.CurrentStep.A2SQuery);
                var a2SResponse = SendA2SRequest(GetCorrectIp(serverInfo), queryPort).GetAwaiter().GetResult();
                serverInfo.PlayerCountText = a2SResponse;

                return;
            }
            case CommandExecutionMethod.Rcon:
            {
                int rconPort = JsonHandler.ExtractRconPort(json, serverInfo.Uuid, serverToMonitor.RconPortVariable, serverInfo.Allocations);
                var rconPassword = serverToMonitor.RconPassword ?? JsonHandler.ExtractRconPassword(json, serverInfo.Uuid, serverToMonitor.RconPasswordVariable);
                
                if (rconPort == 0 || string.IsNullOrWhiteSpace(rconPassword))
                {
                    ConsoleExt.WriteLine($"No RCON port or password found for server: {serverInfo.Name}", ConsoleExt.CurrentStep.RconQuery, ConsoleExt.OutputType.Warning);
                    return;
                }
                
                if (Program.Secrets.ExternalServerIp != null && serverToMonitor.Command != null)
                {
                    var rconResponse = SendRconGameServerCommand(GetCorrectIp(serverInfo), rconPort, rconPassword, serverToMonitor.Command, _gamesToMonitor.First(s => s.Game == serverInfo.Egg.Name).PlayerCountExtractRegex).GetAwaiter().GetResult();
                    serverInfo.PlayerCountText = ServerPlayerCountDisplayCleanup(rconResponse, maxPlayers);
                }
                
                break;
            }
            case CommandExecutionMethod.MinecraftJava:
            {
                int queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable, serverInfo.Allocations);
                
                if (Program.Secrets.ExternalServerIp != null && queryPort != 0)
                {
                    var minecraftResponse = SendJavaMinecraftRequest(GetCorrectIp(serverInfo), queryPort).GetAwaiter().GetResult();
                    ConsoleExt.WriteLine($"Sent Java Minecraft Query to Serer and Port: {Program.Secrets.ExternalServerIp}:{queryPort}", ConsoleExt.CurrentStep.MinecraftJavaQuery, ConsoleExt.OutputType.Debug);
                    ConsoleExt.WriteLine($"Java Minecraft Response: {minecraftResponse}", ConsoleExt.CurrentStep.MinecraftJavaQuery, ConsoleExt.OutputType.Debug);
                    serverInfo.PlayerCountText = minecraftResponse;
                }
                else
                {
                    ConsoleExt.WriteLine("ExternalServerIp or Query Port is null or empty", ConsoleExt.CurrentStep.MinecraftJavaQuery, ConsoleExt.OutputType.Error);
                }
                
                break;
            }
            case CommandExecutionMethod.MinecraftBedrock:
            {
                int queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable, serverInfo.Allocations);
                
                if (Program.Secrets.ExternalServerIp != null && queryPort != 0)
                {
                    var minecraftResponse = SendBedrockMinecraftRequest(GetCorrectIp(serverInfo), queryPort).GetAwaiter().GetResult();
                    ConsoleExt.WriteLine($"Sent Bedrock Minecraft Query to Serer and Port: {Program.Secrets.ExternalServerIp}:{queryPort}", ConsoleExt.CurrentStep.MinecraftBedrockQuery, ConsoleExt.OutputType.Debug);
                    ConsoleExt.WriteLine($"Bedrock Minecraft Response: {minecraftResponse}", ConsoleExt.CurrentStep.MinecraftBedrockQuery, ConsoleExt.OutputType.Debug);
                    serverInfo.PlayerCountText = minecraftResponse;
                }
                else
                {
                    ConsoleExt.WriteLine("ExternalServerIp or Query Port is null or empty", ConsoleExt.CurrentStep.MinecraftBedrockQuery, ConsoleExt.OutputType.Error);
                }
                
                break;
            }
        }
    }
    
    /// <summary>
    /// Runs a Task to continuously get the GamesToMonitor File if continuous reading is enabled.
    /// </summary>
    public static void GetGamesToMonitorFileAsync()
    {
        Task.Run(async () =>
        {
            while (Program.Config.ContinuesGamesToMonitorRead)
            {
                _gamesToMonitor = await FileManager.ReadGamesToMonitorFile();
                await Task.Delay(TimeSpan.FromSeconds(Program.Config.MarkdownUpdateInterval));
            }
        });
    }
}