using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pelican_Keeper.Query_Services;
using RestSharp;

namespace Pelican_Keeper;

using static TemplateClasses;
using static HelperClass;


//TODO: Add a new function for when I want to have multiple functions execute in same succession like in the GetServerList function to keep them on one task and clean up the code.
//Might need to make certain api requests at one specific time before executing those multiple functions to get allocations and check the player count. Just so I keep it to one API request instead of one per function.
//Might also not be a bad idea to create a function to execute those requests and keep those out of the individual functions for cleaner code and segmentation. Which should allow me to also use those responses in other functions if stored global in the class
public static class PelicanInterface
{
    private static List<GamesToMonitor>? _gamesToMonitor = FileManager.ReadGamesToMonitorFile().GetAwaiter().GetResult();
    private static List<EggInfo>? _eggsList;
    private static readonly List<RconService> RconServices = new();
    private static readonly Dictionary<string, DateTime> ShutdownTracker = new();

    public static List<EggInfo>? GetLocalEggList()
    {
        return _eggsList;
    }

    /// <summary>
    /// Gets the entire List of Eggs from the Pelican API
    /// </summary>
    public static void GetEggList()
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/application/eggs");
        var response = CreateRequest(client, Program.Secrets.ClientToken);
        
        try
        {
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                _eggsList = JsonHandler.ExtractEggInfo(response.Content);
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

        try
        {
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var stats = JsonHandler.ExtractServerResources(response.Content);
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

    //TODO: split the server monitoring away from this function to bring it back to 1 function with 1 purpose
    /// <summary>
    /// Gets the Client Server List from the Pelican API, and gets the Network Allocations, and tracks the server for player count and automatic shutdown.
    /// </summary>
    /// <param name="serverInfos">List of ServerInfo</param>
    public static void GetServerAllocations(List<ServerInfo> serverInfos)
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/client/?type=admin-all");
        var response = CreateRequest(client, Program.Secrets.ClientToken);
        
        try
        {
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var allocations = JsonHandler.ExtractNetworkAllocations(response.Content);
                foreach (var serverInfo in serverInfos)
                {
                    serverInfo.Allocations = allocations.Where(s => s.Uuid == serverInfo.Uuid).ToList();
                }
                if (!Program.Config.PlayerCountDisplay) return;
                
                // Only process servers that have allocations (skip bots/services without ports)
                var serversToProcess = Program.Config.IgnoreServersWithoutAllocations
                    ? serverInfos.Where(s => s.Allocations != null && s.Allocations.Count > 0)
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
                        MonitorServers(serverInfo, response.Content);
                        
                        if (Program.Config.AutomaticShutdown)
                        {
                            if (serverInfo.PlayerCountText != "N/A" && !string.IsNullOrEmpty(serverInfo.PlayerCountText))
                            {
                                if (Program.Config.ServersToAutoShutdown != null && Program.Config.ServersToAutoShutdown[0] != "UUIDS HERE" && !Program.Config.ServersToAutoShutdown.Contains(serverInfo.Uuid))
                                {
                                    if (Program.Config.Debug)
                                        ConsoleExt.WriteLine($"Server {serverInfo.Name} is not in the auto-shutdown list. Skipping shutdown check.", ConsoleExt.CurrentStep.PelicanApi);
                                    continue;
                                }
                                
                                if (_gamesToMonitor == null || _gamesToMonitor.Count == 0)
                                {
                                    if (Program.Config.Debug)
                                        ConsoleExt.WriteLine("No game communication configuration found. Skipping shutdown check.", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Warning);
                                    continue;
                                }
                                int playerCount = ExtractPlayerCount(serverInfo.PlayerCountText);
                                if (Program.Config.Debug)
                                    ConsoleExt.WriteLine($"Player count: {playerCount} for server: {serverInfo.Name}", ConsoleExt.CurrentStep.PelicanApi);
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
                                        if (Program.Config.Debug)
                                            ConsoleExt.WriteLine($"Server {serverInfo.Name} is stopping and removed from shutdown tracker.", ConsoleExt.CurrentStep.PelicanApi);
                                    }
                                }
                            }
                        }
                    }
                    else if (isTracked)
                    {
                        ShutdownTracker.Remove(serverInfo.Uuid);
                        if (Program.Config.Debug)
                            ConsoleExt.WriteLine($"Server {serverInfo.Name} is offline or stopping. Removed from shutdown tracker.", ConsoleExt.CurrentStep.PelicanApi);
                    }
                }
                return;
            }

            ConsoleExt.WriteLine("Empty Allocations response content.", ConsoleExt.CurrentStep.PelicanApi);
        }
        catch (JsonException ex)
        {
            ConsoleExt.WriteLine("JSON deserialization or fetching Error: " + ex.Message, ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, ex);
            ConsoleExt.WriteLine("Response content: " + response.Content, ConsoleExt.CurrentStep.PelicanApi);
        }
    }

    //TODO: remove GetEggList and GetServerResourcesList from this list to remove dependencies for those functions
    /// <summary>
    /// Gets the list of servers from the Pelican API
    /// </summary>
    /// <returns>Server list response</returns>
    public static List<ServerInfo> GetServersList()
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/application/servers");
        var response = CreateRequest(client, Program.Secrets.ServerToken);

        try
        {
            if (response.Content != null)
            {
                var servers = JsonHandler.ExtractServerListInfo(response.Content);
                GetEggList();
                foreach (var serverInfo in servers)
                {
                    var foundEgg = _eggsList?.Find(x => x.Id == serverInfo.Egg.Id);
                    if (foundEgg == null) continue;
                    serverInfo.Egg.Name = foundEgg.Name;
                    if (Program.Config.Debug)
                        ConsoleExt.WriteLine($"Egg Name found: {serverInfo.Egg.Name}", ConsoleExt.CurrentStep.PelicanApi);
                }

                string[]? serversToIgnor = Program.Config.ServersToIgnore;
                if (serversToIgnor is { Length: > 0 } && serversToIgnor[0] != "UUIDS HERE") 
                    servers = servers.Where(s => !serversToIgnor.Contains(s.Uuid)).ToList();

                if (Program.Config.IgnoreOfflineServers) 
                    servers = servers.Where(s => s.Resources?.CurrentState.ToLower() != "offline" && s.Resources?.CurrentState.ToLower() != "missing").ToList();
                
                if (Program.Config.IgnoreInternalServers && Program.Config.InternalIpStructure != null)
                {
                    string internalIpPattern = "^" + Regex.Escape(Program.Config.InternalIpStructure).Replace("\\*", "\\d+") + "$";
                    servers = servers.Where(s => s.Allocations != null && s.Allocations.Any(a => !Regex.IsMatch(a.Ip, internalIpPattern))).ToList();
                }
                
                if (Program.Config.LimitServerCount && Program.Config.MaxServerCount > 0)
                {
                    if (Program.Config.ServersToDisplay != null && Program.Config.ServersToDisplay.Length > 0 && Program.Config.ServersToDisplay[0] != "UUIDS HERE")
                        servers = servers.Where(s => Program.Config.ServersToDisplay.Contains(s.Uuid)).ToList();
                    else
                        servers = servers.Take(Program.Config.MaxServerCount).ToList();
                }
                servers = SortServers(servers, Program.Config.MessageSorting, Program.Config.MessageSortingDirection);
                _ = GetServerResourcesList(servers);
                return servers;
            }
            ConsoleExt.WriteLine("Empty Server List response content.", ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error);
        }
        catch (JsonException ex)
        {
            ConsoleExt.WriteLine("JSON deserialization or fetching Error: " + ex.Message, ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, ex);
            ConsoleExt.WriteLine("JSON: " + response.Content, ConsoleExt.CurrentStep.PelicanApi);
        }
        return [];
    }

    //TODO: take the execution of getting the allocations out of this function to reduce dependencies on that function
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
                if (Program.Config.Debug)
                    ConsoleExt.WriteLine("Fetched stats for server: " + server.Name, ConsoleExt.CurrentStep.PelicanApi);
                GetServerResources(server);
            }
            finally { sem.Release(); }
        });
        
        // Run them all
        await Task.WhenAll(statsTasks);
        
        GetServerAllocations(servers);
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
        if (Program.Config.Debug)
            ConsoleExt.WriteLine(response.Content, ConsoleExt.CurrentStep.PelicanApi);
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
            if (Program.Config.Debug)
                ConsoleExt.WriteLine("Reusing existing RCON connection to " + ip + ":" + port, ConsoleExt.CurrentStep.RconQuery);
        }
        else
        {
            if (Program.Config.Debug)
                ConsoleExt.WriteLine("Creating new RCON connection to " + ip + ":" + port, ConsoleExt.CurrentStep.RconQuery);
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
    private static void MonitorServers(ServerInfo serverInfo, string json)
    {
        if (_gamesToMonitor == null || _gamesToMonitor.Count == 0) return;
        
        var serverToMonitor = _gamesToMonitor.FirstOrDefault(s => s.Game == serverInfo.Egg.Name);
        if (serverToMonitor == null)
        {
            if (Program.Config.Debug)
                ConsoleExt.WriteLine("No monitoring configuration found for server: " + serverInfo.Name, ConsoleExt.CurrentStep.GameMonitoring, ConsoleExt.OutputType.Warning);
            return;
        }
        ConsoleExt.WriteLine($"Found Game to Monitor {serverToMonitor.Game}", ConsoleExt.CurrentStep.GameMonitoring);

        int maxPlayers = JsonHandler.ExtractMaxPlayerCount(json, serverInfo.Uuid, serverToMonitor.MaxPlayerVariable, serverToMonitor.MaxPlayer);
        
        switch (serverToMonitor.Protocol)
        {
            case CommandExecutionMethod.A2S:
            {
                int queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable);

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
                int rconPort = JsonHandler.ExtractRconPort(json, serverInfo.Uuid, serverToMonitor.RconPortVariable);
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
                int queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable);
                
                if (Program.Secrets.ExternalServerIp != null && queryPort != 0)
                {
                    var minecraftResponse = SendJavaMinecraftRequest(GetCorrectIp(serverInfo), queryPort).GetAwaiter().GetResult();
                    if (Program.Config.Debug)
                    {
                        ConsoleExt.WriteLine($"Sent Java Minecraft Query to Serer and Port: {Program.Secrets.ExternalServerIp}:{queryPort}", ConsoleExt.CurrentStep.MinecraftJavaQuery);
                        ConsoleExt.WriteLine($"Java Minecraft Response: {minecraftResponse}", ConsoleExt.CurrentStep.MinecraftJavaQuery);
                    }
                    serverInfo.PlayerCountText = minecraftResponse;
                }
                else
                {
                    if (Program.Config.Debug)
                        ConsoleExt.WriteLine("ExternalServerIp or Query Port is null or empty", ConsoleExt.CurrentStep.MinecraftJavaQuery, ConsoleExt.OutputType.Error);
                }
                
                break;
            }
            case CommandExecutionMethod.MinecraftBedrock:
            {
                int queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable);
                
                if (Program.Secrets.ExternalServerIp != null && queryPort != 0)
                {
                    var minecraftResponse = SendBedrockMinecraftRequest(GetCorrectIp(serverInfo), queryPort).GetAwaiter().GetResult();
                    if (Program.Config.Debug)
                    {
                        ConsoleExt.WriteLine($"Sent Bedrock Minecraft Query to Serer and Port: {Program.Secrets.ExternalServerIp}:{queryPort}", ConsoleExt.CurrentStep.MinecraftBedrockQuery);
                        ConsoleExt.WriteLine($"Bedrock Minecraft Response: {minecraftResponse}", ConsoleExt.CurrentStep.MinecraftBedrockQuery);
                    }
                    serverInfo.PlayerCountText = minecraftResponse;
                }
                else
                {
                    if (Program.Config.Debug)
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