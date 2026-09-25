using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pelican_Keeper.Helper_Classes;
using Pelican_Keeper.Interfaces;
using Pelican_Keeper.Query_Services;
using RestSharp;

namespace Pelican_Keeper;

using static TemplateClasses;
using static HelperClass;
using static ExtractorHelpers;
using static ConversionHelpers;

public static class PelicanInterface
{
    private static List<GamesToMonitor>?
        _gamesToMonitor = FileManager.ReadGamesToMonitorFile().GetAwaiter().GetResult();
    
    private static readonly Dictionary<string, DateTime> ShutdownTracker = new();

    private static readonly RestResponse LocalServerListResponse = GetServerList();
    private static readonly List<ServerInfo> ServerListResponse = GetPelicanServerList();
    
    public static void GetConfigFile(ServerInfo serverInfo, string pathToFile)
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/client/" + serverInfo.Uuid + "/files/contents?" +
                                    FilePathConverter(pathToFile));
        var response = CreateRequest(client, Program.Secrets.ClientToken);

        if (!response.IsSuccessStatusCode)
            ConsoleExt.WriteLine("Error: " + $"Status:{response.StatusCode}, Error: {response.ErrorMessage}, Exception: {response.ErrorException}, Response: {response.Content}", ConsoleExt.CurrentStep.PelicanApi,
                ConsoleExt.OutputType.Error, response.ErrorException, true, true);
        //TODO Implement this further to extract the value of the variable, and do this only once on the first run as to conserve API calls and store it for continued use until bot restart
    }

    /// <summary>
    ///     Gets the server resources from the Pelican API
    /// </summary>
    /// <param name="serverInfo">Server Info Class</param>
    /// <returns>The server resources response</returns>
    public static void GetServerResources(ServerInfo serverInfo)
    {
        if (string.IsNullOrWhiteSpace(serverInfo.Uuid))
        {
            ConsoleExt.WriteLine("UUID is null or empty.", ConsoleExt.CurrentStep.PelicanApi,
                ConsoleExt.OutputType.Error);
            return;
        }

        var client =
            new RestClient(Program.Secrets.ServerUrl + "/api/client/servers/" + serverInfo.Uuid + "/resources");
        var response = CreateRequest(client, Program.Secrets.ClientToken);

        if (!response.IsSuccessStatusCode)
            ConsoleExt.WriteLine("Error: " + $"Status:{response.StatusCode}, Error: {response.ErrorMessage}, Exception: {response.ErrorException}, Response: {response.Content}", ConsoleExt.CurrentStep.PelicanApi,
                ConsoleExt.OutputType.Error, response.ErrorException, true, true);

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
            ConsoleExt.WriteLine("JSON deserialization or fetching Error: " + ex.Message,
                ConsoleExt.CurrentStep.PelicanApi);
            ConsoleExt.WriteLine("Response content: " + response.Content, ConsoleExt.CurrentStep.PelicanApi);
        }
    }
    
    private static RestResponse GetServerList()
    {
        var apiExtension = Program.Config.IgnoreOtherUserServers ? "/api/client/?include=egg" : "/api/client/?type=admin-all&include=egg";
        var client = new RestClient(Program.Secrets.ServerUrl + apiExtension);
        var response = CreateRequest(client, Program.Secrets.ClientToken);

        if (!response.IsSuccessStatusCode)
            ConsoleExt.WriteLine("Error: " + $"Status:{response.StatusCode}, Error: {response.ErrorMessage}, Exception: {response.ErrorException}, Response: {response.Content}", ConsoleExt.CurrentStep.PelicanApi,
                ConsoleExt.OutputType.Error, response.ErrorException, true, true);

        if (!string.IsNullOrEmpty(response.Content)) return response;
        ConsoleExt.WriteLine($"Server List Response is null or empty. Response Content: {response.Content}",
            ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, response.ErrorException, true, true);
        throw new Exception("Server List Response is null or empty.");
    }

    private static void MonitorServers(List<ServerInfo> serverInfos)
    {
        if (!Program.Config.PlayerCountDisplay) return;
        if (serverInfos.Count == 0)
            ConsoleExt.WriteLine("Servers list is empty.", ConsoleExt.CurrentStep.PelicanApi,
                ConsoleExt.OutputType.Error);

        // Only process servers that have allocations (skip bots/services without ports)
        var serversToProcess = Program.Config.IgnoreServersWithoutAllocations
            ? serverInfos.Where(s => s.Allocations is { Count: > 0 })
            : serverInfos;

        foreach (var serverInfo in serversToProcess)
        {
            var isTracked = ShutdownTracker.Any(x => x.Key == serverInfo.Uuid);
            var serverState = serverInfo.Resources?.CurrentState;
            if (serverState != ServerStatus.Offline && serverState != ServerStatus.Stopping && serverState != ServerStatus.Starting &&
                serverState != ServerStatus.Missing)
            {
                if (!isTracked)
                {
                    ShutdownTracker[serverInfo.Uuid] = DateTime.Now;
                    ConsoleExt.WriteLine($"{serverInfo.Name} is tracked for shutdown: {isTracked}",
                        ConsoleExt.CurrentStep.PelicanApi);
                }

                RequestToMonitoringServers(serverInfo, LocalServerListResponse.Content!);

                if (Program.Config.AutomaticShutdown)
                    if (serverInfo.PlayerCountText != "N/A" && !string.IsNullOrEmpty(serverInfo.PlayerCountText))
                    {
                        if (Program.Config.ServersToAutoShutdown != null &&
                            Program.Config.ServersToAutoShutdown[0] != "UUIDS HERE" &&
                            !Program.Config.ServersToAutoShutdown.Contains(serverInfo.Uuid))
                        {
                            ConsoleExt.WriteLine(
                                $"Server {serverInfo.Name} is not in the auto-shutdown list. Skipping shutdown check.",
                                ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
                            continue;
                        }

                        if (_gamesToMonitor == null || _gamesToMonitor.Count == 0)
                        {
                            ConsoleExt.WriteLine("No game communication configuration found. Skipping shutdown check.",
                                ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Warning);
                            continue;
                        }

                        var playerCount = ExtractPlayerCount(serverInfo.PlayerCountText);
                        ConsoleExt.WriteLine($"Player count: {playerCount} for server: {serverInfo.Name}",
                            ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
                        if (playerCount > 0)
                        {
                            ShutdownTracker[serverInfo.Uuid] = DateTime.Now;
                        }
                        else
                        {
                            TimeSpan.TryParseExact(Program.Config.EmptyServerTimeout, @"dd\:hh\:mm",
                                CultureInfo.InvariantCulture, out var timeTillShutdown);
                            if (timeTillShutdown == TimeSpan.Zero)
                                timeTillShutdown = TimeSpan.FromHours(1);
                            if (DateTime.Now - ShutdownTracker[serverInfo.Uuid] >= timeTillShutdown)
                            {
                                SendPowerCommand(serverInfo.Uuid, "stop");
                                ConsoleExt.WriteLine(
                                    $"Server {serverInfo.Name} has been empty for over an hour. Sending shutdown command.",
                                    ConsoleExt.CurrentStep.PelicanApi);
                                ShutdownTracker.Remove(serverInfo.Uuid);
                                ConsoleExt.WriteLine(
                                    $"Server {serverInfo.Name} is stopping and removed from shutdown tracker.",
                                    ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
                            }
                        }
                    }
            }
            else if (isTracked)
            {
                ShutdownTracker.Remove(serverInfo.Uuid);
                ConsoleExt.WriteLine($"Server {serverInfo.Name} is offline or stopping. Removed from shutdown tracker.",
                    ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Debug);
            }
        }
    }

    /// <summary>
    ///     Gets the list of servers from the Pelican API
    /// </summary>
    /// <returns>Server Info list</returns>
    private static List<ServerInfo> GetPelicanServerList()
    {
        if (string.IsNullOrEmpty(LocalServerListResponse.Content) ||
            string.IsNullOrWhiteSpace(LocalServerListResponse.Content)) return [];
        try
        {
            return JsonHandler.ExtractServerListInfo(LocalServerListResponse.Content);
        }
        catch (JsonException ex)
        {
            ConsoleExt.WriteLine("JSON deserialization or fetching Error: " + ex.Message,
                ConsoleExt.CurrentStep.PelicanApi, ConsoleExt.OutputType.Error, ex);
            ConsoleExt.WriteLine("JSON: " + LocalServerListResponse.Content, ConsoleExt.CurrentStep.PelicanApi);
        }
        return [];
    }

    /// <summary>
    ///     Processed the Server list with the settings defined in the config
    /// </summary>
    /// <param name="servers">Server Info list</param>
    private static List<ServerInfo> ProcessServerList(List<ServerInfo> servers)
    {
        var serversToIgnore = Program.Config.ServersToIgnore;
        if (serversToIgnore is { Length: > 0 } && serversToIgnore[0] != "UUIDS HERE")
            servers = servers.Where(s => !serversToIgnore.Contains(s.Uuid)).ToList();

        _ = GetServerResourcesList(servers);
        if (Program.Config.IgnoreOfflineServers)
            servers = servers.Where(s =>
                    s.Resources?.CurrentState != ServerStatus.Offline &&
                    s.Resources?.CurrentState != ServerStatus.Missing)
                .ToList();

        servers = SortServers(servers, Program.Config.MessageSorting, Program.Config.MessageSortingDirection);
        
        if (Program.Config.IgnoreInternalServers && Program.Config.InternalIpStructure != null)
        {
            var internalIpPattern = "^" + Regex.Escape(Program.Config.InternalIpStructure).Replace("\\*", "\\d+") + "$";
            servers = servers.Where(s => !(s.Allocations?.All(a => Regex.IsMatch(a.Ip, internalIpPattern)) ?? false))
                .ToList();
        }

        if (!Program.Config.LimitServerCount || Program.Config.MaxServerCount <= 0 ||
            Program.Config.MessageFormat == MessageFormat.Paginated) return servers;
        
        if (Program.Config.ServersToDisplay != null && Program.Config.ServersToDisplay.Length > 0 &&
            Program.Config.ServersToDisplay[0] != "UUIDS HERE")
            servers = servers.Where(s => Program.Config.ServersToDisplay.Contains(s.Uuid)).ToList();
        else
            servers = servers.Take(Program.Config.MaxServerCount).ToList();

        return servers;
    }

    /// <summary>
    ///     Returns a ServerInfo List which has been processed and been filled with all its information
    /// </summary>
    /// <returns>Server list response</returns>
    public static List<ServerInfo> GetServersList()
    {
        var serverInfos = GetPelicanServerList();
        serverInfos = ProcessServerList(serverInfos);
        _ = GetServerResourcesList(ServerListResponse);
        MonitorServers(serverInfos);
        return serverInfos;
    }

    /// <summary>
    ///     Gets alist of server resources from the Pelican API
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
                ConsoleExt.WriteLine("Fetched stats for server: " + server.Name, ConsoleExt.CurrentStep.PelicanApi,
                    ConsoleExt.OutputType.Debug);
                GetServerResources(server);
            }
            finally
            {
                sem.Release();
            }
        });

        // Run them all
        await Task.WhenAll(statsTasks);
    }

    /// <summary>
    ///     Sends a Power command to the specified Server.
    /// </summary>
    /// <param name="uuid">UUID of the Server</param>
    /// <param name="command">Command to send ("start", "stop", etc.)</param>
    public static void SendPowerCommand(string? uuid, string command)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            ConsoleExt.WriteLine("UUID is null or empty.", ConsoleExt.CurrentStep.PelicanApi,
                ConsoleExt.OutputType.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            ConsoleExt.WriteLine("Command is null or empty.", ConsoleExt.CurrentStep.PelicanApi,
                ConsoleExt.OutputType.Error);
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
    ///     Sends a request depending on the CommandExecutionMethod being used to the specified IP and Port
    /// </summary>
    /// <param name="ip">IP of the Server</param>
    /// <param name="port">Port of the Server</param>
    /// <param name="executionMethod">Execution method being used to establish the connection and send the request</param>
    /// <param name="password">RCON Password of the Server</param>
    /// <param name="command">Game command to send</param>
    /// <param name="regexPattern">Regex Pattern to use when extracting player count</param>
    /// <returns>The response to the command that was sent</returns>
    public static async Task<string> SendServerRequest(string ip, int port, CommandExecutionMethod executionMethod,
        string? password = null, string? command = null, string? regexPattern = null)
    {
        ISendCommand connectionClass = null!;
        switch (executionMethod)
        {
            case CommandExecutionMethod.Rcon:
                connectionClass = new RconService(ip, port, password!);
                break;
            case CommandExecutionMethod.A2S:
                connectionClass = new A2SService(ip, port);
                break;
            case CommandExecutionMethod.MinecraftJava:
                connectionClass = new JavaMinecraftQueryService(ip, port);
                break;
            case CommandExecutionMethod.MinecraftBedrock:
                connectionClass = new BedrockMinecraftQueryService(ip, port);
                break;
            case CommandExecutionMethod.MinecraftMixed:
                connectionClass = new BedrockMinecraftQueryService(ip, port);
                break;
            case CommandExecutionMethod.Terraria:
                connectionClass = new TShock(ip, port);// Not Implemented
                break;
        }

        await connectionClass.Connect();
        var response = await connectionClass.SendCommandAsync(command, regexPattern);
        if (string.IsNullOrEmpty(response) && executionMethod == CommandExecutionMethod.MinecraftMixed)
        {
            ConsoleExt.WriteLine("Could not connect to server using Bedrock Minecraft Query Service. Trying Java Minecraft Query Service.", ConsoleExt.CurrentStep.GameMonitoring, ConsoleExt.OutputType.Warning);
            connectionClass.Dispose();
            connectionClass = new JavaMinecraftQueryService(ip, port);
            await connectionClass.Connect();
            response = await connectionClass.SendCommandAsync(command, regexPattern);
        }
        connectionClass.Dispose();
        return response;
    }

    /// <summary>
    ///     Monitors a specified Server and getting the Player count, Max player count, and put that into a neat text
    /// </summary>
    /// <param name="serverInfo">The ServerInfo of the specific server</param>
    /// <param name="json">Input JSON</param>
    private static void RequestToMonitoringServers(ServerInfo serverInfo, string json)
    {
        if (_gamesToMonitor == null || _gamesToMonitor.Count == 0) return;

        var serverToMonitor = _gamesToMonitor.FirstOrDefault(s => s.Game == serverInfo.EggName);
        if (serverToMonitor == null)
        {
            ConsoleExt.WriteLine("No monitoring configuration found for server: " + serverInfo.Name,
                ConsoleExt.CurrentStep.GameMonitoring, ConsoleExt.OutputType.Warning);
            return;
        }

        ConsoleExt.WriteLine($"Found Game to Monitor {serverToMonitor.Game}", ConsoleExt.CurrentStep.GameMonitoring);

        var maxPlayers = JsonHandler.ExtractMaxPlayerCount(json, serverInfo.Uuid, serverToMonitor.MaxPlayerVariable,
            serverToMonitor.MaxPlayer);

        if (serverToMonitor.Protocol == CommandExecutionMethod.Terraria)
        {
            ConsoleExt.WriteLine("Terraria query protocol not implemented yet!", ConsoleExt.CurrentStep.GameMonitoring, ConsoleExt.OutputType.Error);
            return;
        }
        int queryPort;
        string rconPassword = string.Empty;
        if (serverToMonitor.Protocol == CommandExecutionMethod.Rcon)
        {
            queryPort = JsonHandler.ExtractRconPort(json, serverInfo.Uuid, serverToMonitor.RconPortVariable, serverInfo.Allocations);
            rconPassword = serverToMonitor.RconPassword ??
                           JsonHandler.ExtractRconPassword(json, serverInfo.Uuid,
                               serverToMonitor
                                   .RconPasswordVariable); // TODO: Check if the Config Location has been set and extract the password from there if the location and variable is set in the games to monitor

            if (queryPort == 0 || string.IsNullOrWhiteSpace(rconPassword))
            {
                ConsoleExt.WriteLine($"No RCON port or password found for server: {serverInfo.Name}",
                    ConsoleExt.CurrentStep.ServerQuery, ConsoleExt.OutputType.Warning);
                return;
            }
        }
        else
        {
            queryPort = JsonHandler.ExtractQueryPort(json, serverInfo.Uuid, serverToMonitor.QueryPortVariable,
                serverInfo.Allocations);
        }

        if (queryPort == 0)
        {
            ConsoleExt.WriteLine("No Query port found for server: " + serverInfo.Name,
                ConsoleExt.CurrentStep.ServerQuery, ConsoleExt.OutputType.Warning);
            return;
        }
        if (Program.Secrets.ExternalServerIp == null)
        {
            ConsoleExt.WriteLine("ExternalServerIp is null", ConsoleExt.CurrentStep.ServerQuery, ConsoleExt.OutputType.Warning);
            return;
        }
                
        var serverResponse = SendServerRequest(GetCorrectIp(serverInfo), queryPort, serverToMonitor.Protocol, rconPassword, serverToMonitor.Command,
                _gamesToMonitor.First(s => s.Game == serverInfo.EggName).PlayerCountExtractRegex)
            .GetAwaiter().GetResult();
        ConsoleExt.WriteLine(
            $"Sent {serverToMonitor.Protocol} Query to Serer and Port: {Program.Secrets.ExternalServerIp}:{queryPort}",
            ConsoleExt.CurrentStep.ServerQuery, ConsoleExt.OutputType.Debug);
        ConsoleExt.WriteLine($"Server Response: {serverResponse}",
            ConsoleExt.CurrentStep.ServerQuery, ConsoleExt.OutputType.Debug);
        serverInfo.PlayerCountText = serverToMonitor.Protocol == CommandExecutionMethod.Rcon ? ServerPlayerCountDisplayCleanup(serverResponse, maxPlayers) : serverResponse;
    }

    /// <summary>
    ///     Runs a Task to continuously get the GamesToMonitor File if continuous reading is enabled.
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