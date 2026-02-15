using System.Text.Json;
using System.Text.RegularExpressions;
using static Pelican_Keeper.TemplateClasses;

namespace Pelican_Keeper;

public static class JsonHandler
{
    /// <summary>
    /// Extracts a list of Eggs from the input JSON
    /// </summary>
    /// <param name="json">Input JSON</param>
    /// <returns>List of EggInfo that includes the ID and Name</returns>
    internal static List<EggInfo> ExtractEggInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        List<EggInfo> eggs = new List<EggInfo>();
        var eggArray = root.GetPropertySafe("data").EnumerateArray();
        ConsoleExt.WriteLine($"Egg List: {string.Join(", ", eggArray.Select(x => x.GetPropertySafe("name").GetString()))}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
        
        foreach (var egg in eggArray)
        {
            var attr = egg.GetPropertySafe("attributes");
            string name = attr.GetPropertySafe("name").GetString() ?? string.Empty;
            int id = attr.GetPropertySafe("id").GetInt32();
            ConsoleExt.WriteLine($"Egg Name: {name}, Egg ID: {id}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);

            eggs.Add(new EggInfo { Id = id, Name = name });
        }

        return eggs;
    }

    /// <summary>
    /// Extracts the RCON Port from the Input JSON
    /// </summary>
    /// <param name="json">Input JSON</param>
    /// <param name="uuid">UUID of the Server</param>
    /// <param name="variableName">Variable Name of the RCON Port in the Pelican Panel</param>
    /// <param name="serverAllocations">List of Allocations of the Server</param>
    /// <returns>The RCON port if found or 0 if not found</returns>
    internal static int ExtractRconPort(string json, string uuid, string? variableName, List<ServerAllocation>? serverAllocations)
    {
        int rconPort = 0;
        if (variableName != null)
        {
            var match = Regex.Match(variableName, @"SERVER_PORT\s*\+\s*(\d+)");
            if (match.Success)
            {
                int addition = Convert.ToInt32(match.Groups[1].Value);
                
                rconPort = serverAllocations != null ? serverAllocations.Find(x => x.IsDefault)!.Port + addition : 0;
                ConsoleExt.WriteLine($"[ExtractRconPort] Regex Extracted Rcon Port: {rconPort}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
                return rconPort;
            }
        }
        if (variableName is "SERVER_PORT")
        {
            rconPort = serverAllocations != null ? serverAllocations.Find(x => x.IsDefault)!.Port : 0;
            ConsoleExt.WriteLine($"[ExtractRconPort] Server Port Extracted Rcon Port: {rconPort}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
            return rconPort;
        }
        if (variableName == null || variableName.Trim() == string.Empty) variableName = "RCON_PORT";
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        var serversArray = root.GetPropertySafe("data").EnumerateArray();
        foreach (var data in serversArray)
        {
            var serverUuid = data.GetPropertySafe("attributes").GetPropertySafe("uuid").ToString();
            if (serverUuid != uuid) continue;
            ConsoleExt.WriteLine($"[ExtractRconPort] Server UUID: {serverUuid}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
            var variablesArray = data.GetPropertySafe("attributes").GetPropertySafe("relationships").GetPropertySafe("variables").GetPropertySafe("data");
        
            foreach (var alloc in variablesArray.EnumerateArray())
            {
                var attr = alloc.GetPropertySafe("attributes");
                if (attr.GetPropertySafe("env_variable").GetString() == variableName &&
                    int.TryParse(attr.GetPropertySafe("server_value").GetString(), out rconPort))
                {
                    ConsoleExt.WriteLine($"[ExtractRconPort] Extracted Rcon Port: {rconPort}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
                }
            }
        }

        return rconPort;
    }
    
    /// <summary>
    /// Extracts the RCON Password from the Input JSON
    /// </summary>
    /// <param name="json">Input JSON</param>
    /// <param name="uuid">UUID of the Server</param>
    /// <param name="variableName">Variable Name of the RCON Password in the Pelican Panel</param>
    /// <returns>String of The Password if found or Empty string if not found</returns>
    internal static string ExtractRconPassword(string json, string uuid, string? variableName)
    {
        if (variableName == null || variableName.Trim() == string.Empty) variableName = "RCON_PASS";
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string rconPassword = string.Empty;
        var serversArray = root.GetPropertySafe("data").EnumerateArray();
        foreach (var data in serversArray)
        {
            var serverUuid = data.GetPropertySafe("attributes").GetPropertySafe("uuid").ToString();
            if (serverUuid != uuid) continue;
            ConsoleExt.WriteLine($"[ExtractRconPassword] Server UUID: {serverUuid}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
            var variablesArray = data.GetPropertySafe("attributes").GetPropertySafe("relationships").GetPropertySafe("variables").GetPropertySafe("data");
        
            foreach (var alloc in variablesArray.EnumerateArray())
            {
                var attr = alloc.GetPropertySafe("attributes");
                if (attr.GetPropertySafe("env_variable").GetString() == variableName) rconPassword = attr.GetPropertySafe("server_value").GetString() ?? string.Empty;
                ConsoleExt.WriteLine(
                    string.IsNullOrEmpty(rconPassword)
                        ? "[ExtractRconPassword] Rcon Password is Null or Empty"
                        : $"[ExtractRconPassword] Rcon Password Lenght: {rconPassword.Length}",
                    ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
            }
        }

        return rconPassword;
    }

    /// <summary>
    /// Extracts the Query Port from the Input JSON
    /// </summary>
    /// <param name="json">Input JSON</param>
    /// <param name="uuid">UUID of the Server</param>
    /// <param name="variableName">Variable Name of the Query Port in the Pelican Panel</param>
    /// <param name="serverAllocations">List of Allocations of the Server</param>
    /// <returns>The Query port if found or 0 if not found</returns>
    internal static int ExtractQueryPort(string json, string uuid, string? variableName, List<ServerAllocation>? serverAllocations)
    {
        int queryPort = 0;
        
        if (variableName != null)
        {
            var match = Regex.Match(variableName, @"SERVER_PORT\s*\+\s*(\d+)");
            if (match.Success)
            {
                int addition = Convert.ToInt32(match.Groups[1].Value);

                queryPort = serverAllocations != null ? serverAllocations.Find(x => x.IsDefault)!.Port + addition : 0;
                ConsoleExt.WriteLine($"[ExtractQueryPort] Regex Extracted Query Port: {queryPort}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
                return queryPort;
            }
        }
        if (variableName is "SERVER_PORT")
        {
            queryPort = serverAllocations != null ? serverAllocations.Find(x => x.IsDefault)!.Port : 0;
            ConsoleExt.WriteLine($"[ExtractQueryPort] Server Port Extracted Query Port: {queryPort}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
            return queryPort;
        }
        if (variableName == null || variableName.Trim() == string.Empty) variableName = "QUERY_PORT";
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        var serversArray = root.GetPropertySafe("data").EnumerateArray();
        foreach (var data in serversArray)
        {
            var serverUuid = data.GetPropertySafe("attributes").GetPropertySafe("uuid").ToString();
            if (serverUuid != uuid) continue;
            ConsoleExt.WriteLine($"[ExtractQueryPort] Server UUID: {serverUuid}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
            var variablesArray = data.GetPropertySafe("attributes").GetPropertySafe("relationships").GetPropertySafe("variables").GetPropertySafe("data");
        
            foreach (var alloc in variablesArray.EnumerateArray())
            {
                var attr = alloc.GetPropertySafe("attributes");
                if (attr.GetPropertySafe("env_variable").GetString() == variableName &&
                    int.TryParse(attr.GetPropertySafe("server_value").GetString(), out queryPort))
                {
                    ConsoleExt.WriteLine($"[ExtractQueryPort] Extracted Quert Port: {queryPort}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
                }
            }
        }

        return queryPort;
    }

    /// <summary>
    /// Extracts the max player count from the Input JSON
    /// </summary>
    /// <param name="json">Input JSON</param>
    /// <param name="uuid">UUID of the Server</param>
    /// <param name="variableName">Variable Name of the Max Player Count in the Pelican Panel</param>
    /// <param name="maxPlayer">Optional! if max player is set, it uses that instead of the variable</param>
    /// <returns>The Max Player Count if found or 0 if not found</returns>
    public static int ExtractMaxPlayerCount(string json, string uuid, string? variableName, string? maxPlayer)
    {
        if (!string.IsNullOrEmpty(maxPlayer))
            if (int.TryParse(maxPlayer, out int intMaxPlayers))
            {
                ConsoleExt.WriteLine($"[ExtractMaxPlayerCount] Max Player Count: {intMaxPlayers}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
                return intMaxPlayers;
            }
        
        if (variableName == null || variableName.Trim() == string.Empty) variableName = "MAX_PLAYERS";
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        int maxPlayers = 0;
        var serversArray = root.GetPropertySafe("data").EnumerateArray();
        foreach (var data in serversArray)
        {
            var serverUuid = data.GetPropertySafe("attributes").GetPropertySafe("uuid").ToString();
            if (serverUuid != uuid) continue;
            ConsoleExt.WriteLine($"[ExtractMaxPlayerCount] Server UUID: {serverUuid}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
            var variablesArray = data.GetPropertySafe("attributes").GetPropertySafe("relationships").GetPropertySafe("variables").GetPropertySafe("data");
        
            foreach (var alloc in variablesArray.EnumerateArray())
            {
                var attr = alloc.GetPropertySafe("attributes");
                if (attr.GetPropertySafe("env_variable").GetString() == variableName &&
                    int.TryParse(attr.GetPropertySafe("server_value").GetString(), out maxPlayers))
                {
                    ConsoleExt.WriteLine($"[ExtractMaxPlayerCount] Extracted Max Player Count: {maxPlayers}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
                }
            }
        }
        
        return maxPlayers;
    }

    /// <summary>
    /// Extracts the Network Allocations from the Input JSON
    /// </summary>
    /// <param name="json">Input JSON</param>
    /// <param name="serverUuid">Optional! UUID of the server you want to extract the Network allocations from</param>
    /// <returns>List of ServerAllocation</returns>
    internal static List<ServerAllocation> ExtractNetworkAllocations(string json, string? serverUuid = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Extract allocations
        var allocations = new List<ServerAllocation>();
        var serversArray = root.GetPropertySafe("data").EnumerateArray();

        foreach (var data in serversArray)
        {
            var attr = data.GetPropertySafe("attributes");
            var uuid = attr.GetPropertySafe("uuid").GetString() ?? string.Empty;
            
            if (uuid != serverUuid && serverUuid != null) continue;
            ConsoleExt.WriteLine($"[ExtractMaxPlayerCount] Server UUID: {uuid}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
            
            var allocationsArray = attr.GetPropertySafe("relationships").GetPropertySafe("allocations").GetPropertySafe("data").EnumerateArray();
            foreach (var alloc in allocationsArray)
            {
                var attrib = alloc.GetPropertySafe("attributes");
                var ip = attrib.GetPropertySafe("ip").GetString() ?? string.Empty;
                var port = attrib.GetPropertySafe("port").GetInt32();
                var isDefault = attrib.GetPropertySafe("is_default").GetBoolean();

                ServerAllocation allocation = new ServerAllocation
                {
                    Uuid = uuid,
                    Ip = ip,
                    Port = port,
                    IsDefault = isDefault
                };
                
                allocations.Add(allocation);
                ConsoleExt.WriteLine($"[ExtractNetworkAllocations] Network Allocation Added UUID: {allocation.Uuid}, IP: {allocation.Ip}, Port: {allocation.Port},  IsDefault: {allocation.IsDefault}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
            }
        }
        
        return allocations;
    }
    
    /// <summary>
    /// Extracts the Server List from the Input JSON
    /// </summary>
    /// <param name="json">Input JSON</param>
    /// <returns>List of ServerInfo of the extracted Servers</returns>
    internal static List<ServerInfo> ExtractServerListInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var serverInfo = new List<ServerInfo>();
        var serversArray = root.GetPropertySafe("data").EnumerateArray();
        foreach (var server in serversArray)
        {
            var id = server.GetPropertySafe("attributes").GetPropertySafe("id").GetInt32();
            var uuid = server.GetPropertySafe("attributes").GetPropertySafe("uuid").GetString() ?? string.Empty;
            var name = server.GetPropertySafe("attributes").GetPropertySafe("name").GetString() ?? string.Empty;
            var egg = server.GetPropertySafe("attributes").GetPropertySafe("egg").GetInt32();
            
            var maxMemory = server.GetPropertySafe("attributes").GetPropertySafe("limits").GetPropertySafe("memory").GetInt32();
            var maxCpu = server.GetPropertySafe("attributes").GetPropertySafe("limits").GetPropertySafe("cpu").GetInt32();
            var maxDisk = server.GetPropertySafe("attributes").GetPropertySafe("limits").GetPropertySafe("disk").GetInt32();
            
            serverInfo.Add(new ServerInfo {
                Id = id,
                Uuid = uuid,
                Name = name,
                Egg = new EggInfo { Id = egg },
                Resources = new ServerResources {
                    MemoryMaximum = maxMemory,
                    DiskMaximum = maxDisk,
                    CpuMaximum = maxCpu
                }
            });
            
            ConsoleExt.WriteLine($"[ExtractServerListInfo] Server Info Added ID: {id}, UUID: {uuid}, Server Name: {name}, Egg ID: {egg}, Max Memory: {maxMemory}, Max CPU: {maxCpu}, Max Disk: {maxDisk}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);
        }

        return serverInfo;
    }
    
    /// <summary>
    /// Extracts the Server Resources from the Input JSON
    /// </summary>
    /// <param name="json">Input JSON</param>
    /// <returns>Server Resources of that Server</returns>
    internal static ServerResources ExtractServerResources(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        JsonElement attributes = root.GetPropertySafe("attributes");
        JsonElement resources = attributes.GetPropertySafe("resources");

        string currentState = attributes.GetPropertySafe("current_state").GetString() ?? string.Empty;
            
        long memory = resources.GetPropertySafe("memory_bytes").GetInt64();
        double cpu = resources.GetPropertySafe("cpu_absolute").GetDouble();
        long disk = resources.GetPropertySafe("disk_bytes").GetInt64();
        long networkRx = resources.GetPropertySafe("network_rx_bytes").GetInt64();
        long networkTx = resources.GetPropertySafe("network_tx_bytes").GetInt64();
        long uptime = resources.GetPropertySafe("uptime").GetInt64();
            
        var resourcesInfo = new ServerResources {
            CurrentState = currentState,
            MemoryBytes = memory,
            CpuAbsolute = cpu,
            DiskBytes = disk,
            NetworkRxBytes = networkRx,
            NetworkTxBytes = networkTx,
            Uptime = uptime
        };
        
        ConsoleExt.WriteLine($"[ExtractServerResources] Server Resources Extracted Current State: {currentState}, Memory Bytes: {memory}, CPU Abolute: {cpu}, Disk Bytes: {disk}, Network Rx Bytes {networkRx}, Network Tx Bytes: {networkTx}, Uptime: {uptime}", ConsoleExt.CurrentStep.JsonProcessing, ConsoleExt.OutputType.Debug);

        return resourcesInfo;
    }
    
    /// <summary>
    /// Tries to find the Property by name in a JSON by recursively looking through it's Elements
    /// </summary>
    /// <param name="element">JSON Element to start searching</param>
    /// <param name="propertyName">Name of the Property it's trying to find</param>
    /// <returns>The JSON Element if found or null if not</returns>
    private static JsonElement? FindProperty(this JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName))
                    return property.Value;

                var found = FindProperty(property.Value, propertyName);
                if (found.HasValue)
                    return found;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var found = FindProperty(item, propertyName);
                if (found.HasValue)
                    return found;
            }
        }

        return null;
    }
    
    /// <summary>
    /// Tries to get the Property in a JSON Element by name using the TryGetProperty function and falls back to FindProperty if nothing was found on the first try
    /// </summary>
    /// <param name="element">JSON Element to search through</param>
    /// <param name="propertyName">Name of the Property</param>
    /// <returns>The found JSON Element</returns>
    /// <exception cref="Exception">Property wasn't found</exception>
    private static JsonElement GetPropertySafe(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
            return value;

        var fallback = element.FindProperty(propertyName);
        if (fallback.HasValue)
            return fallback.Value;

        throw new Exception($"Property '{propertyName}' not found in JSON.");
    }
}