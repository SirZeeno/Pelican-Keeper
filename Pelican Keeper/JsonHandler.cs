using System.Text.Json;
using System.Text.RegularExpressions;
using static Pelican_Keeper.TemplateClasses;

namespace Pelican_Keeper;

public static class JsonHandler
{
    internal static List<EggInfo> ExtractEggInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        List<EggInfo> eggs = new List<EggInfo>();
        var eggArray = root.GetProperty("data").EnumerateArray();
        foreach (var egg in eggArray)
        {
            var attr = egg.GetProperty("attributes");
            string name = attr.GetProperty("name").GetString() ?? string.Empty;
            int id = attr.GetProperty("id").GetInt32();
            eggs.Add(new EggInfo { Id = id, Name = name });
        }
        return eggs;
    }

    internal static int ExtractRconPort(string json, string uuid, string? variableName)
    {
        if (variableName != null)
        {
            var match = Regex.Match(variableName, @"SERVER_PORT\s*\+\s*(\d+)");
            if (match.Success)
            {
                int addition = Convert.ToInt32(match.Groups[1].Value);
                var allocations = ExtractNetworkAllocations(json, uuid);
                return allocations.Find(x => x.IsDefault)!.Port + addition;
            }
        }
        if (variableName is "SERVER_PORT")
        {
            var allocations = ExtractNetworkAllocations(json, uuid);
            return allocations.Find(x => x.IsDefault)!.Port;
        }
        if (variableName == null || variableName.Trim() == string.Empty) variableName = "RCON_PORT";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        int rconPort = 0;
        var serversArray = root.GetProperty("data").EnumerateArray();
        foreach (var data in serversArray)
        {
            var serverUuid = data.GetProperty("attributes").GetProperty("uuid").ToString();
            if (serverUuid != uuid) continue;
            var variablesArray = data.GetProperty("attributes").GetProperty("relationships").GetProperty("variables").GetProperty("data");
            foreach (var alloc in variablesArray.EnumerateArray())
            {
                var attr = alloc.GetProperty("attributes");
                if (attr.GetProperty("env_variable").GetString() == variableName && int.TryParse(attr.GetProperty("server_value").GetString(), out rconPort)) { }
            }
        }
        return rconPort;
    }

    internal static string ExtractRconPassword(string json, string uuid, string? variableName)
    {
        if (variableName == null || variableName.Trim() == string.Empty) variableName = "RCON_PASS";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        string rconPassword = string.Empty;
        var serversArray = root.GetProperty("data").EnumerateArray();
        foreach (var data in serversArray)
        {
            var serverUuid = data.GetProperty("attributes").GetProperty("uuid").ToString();
            if (serverUuid != uuid) continue;
            var variablesArray = data.GetProperty("attributes").GetProperty("relationships").GetProperty("variables").GetProperty("data");
            foreach (var alloc in variablesArray.EnumerateArray())
            {
                var attr = alloc.GetProperty("attributes");
                if (attr.GetProperty("env_variable").GetString() == variableName) rconPassword = attr.GetProperty("server_value").GetString() ?? string.Empty;
            }
        }
        return rconPassword;
    }

    internal static int ExtractQueryPort(string json, string uuid, string? variableName)
    {
        if (variableName != null)
        {
            var match = Regex.Match(variableName, @"SERVER_PORT\s*\+\s*(\d+)");
            if (match.Success)
            {
                int addition = Convert.ToInt32(match.Groups[1].Value);
                var allocations = ExtractNetworkAllocations(json, uuid);
                return allocations.Find(x => x.IsDefault)!.Port + addition;
            }
        }
        if (variableName is "SERVER_PORT")
        {
            var allocations = ExtractNetworkAllocations(json, uuid);
            return allocations.Find(x => x.IsDefault)!.Port;
        }
        if (variableName == null || variableName.Trim() == string.Empty) variableName = "QUERY_PORT";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        int queryPort = 0;
        var serversArray = root.GetProperty("data").EnumerateArray();
        foreach (var data in serversArray)
        {
            var serverUuid = data.GetProperty("attributes").GetProperty("uuid").ToString();
            if (serverUuid != uuid) continue;
            var variablesArray = data.GetProperty("attributes").GetProperty("relationships").GetProperty("variables").GetProperty("data");
            foreach (var alloc in variablesArray.EnumerateArray())
            {
                var attr = alloc.GetProperty("attributes");
                if (attr.GetProperty("env_variable").GetString() == variableName && int.TryParse(attr.GetProperty("server_value").GetString(), out queryPort)) { }
            }
        }
        return queryPort;
    }

    public static int ExtractMaxPlayerCount(string json, string uuid, string? variableName, string? maxPlayer)
    {
        if (!string.IsNullOrEmpty(maxPlayer)) { if (int.TryParse(maxPlayer, out int intMaxPlayers)) return intMaxPlayers; }
        if (variableName == null || variableName.Trim() == string.Empty) variableName = "MAX_PLAYERS";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        int maxPlayers = 0;
        var serversArray = root.GetProperty("data").EnumerateArray();
        foreach (var data in serversArray)
        {
            var serverUuid = data.GetProperty("attributes").GetProperty("uuid").ToString();
            if (serverUuid != uuid) continue;
            var variablesArray = data.GetProperty("attributes").GetProperty("relationships").GetProperty("variables").GetProperty("data");
            foreach (var alloc in variablesArray.EnumerateArray())
            {
                var attr = alloc.GetProperty("attributes");
                if (attr.GetProperty("env_variable").GetString() == variableName && int.TryParse(attr.GetProperty("server_value").GetString(), out maxPlayers)) { }
            }
        }
        return maxPlayers;
    }

    internal static List<ServerAllocation> ExtractNetworkAllocations(string json, string? serverUuid = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var allocations = new List<ServerAllocation>();
        var serversArray = root.GetProperty("data").EnumerateArray();
        foreach (var data in serversArray)
        {
            var attr = data.GetProperty("attributes");
            var uuid = attr.GetProperty("uuid").GetString() ?? string.Empty;
            if ((serverUuid == null || uuid != serverUuid) && serverUuid != null) continue;
            var allocationsArray = attr.GetProperty("relationships").GetProperty("allocations").GetProperty("data").EnumerateArray();
            foreach (var alloc in allocationsArray)
            {
                var attrib = alloc.GetProperty("attributes");
                var ip = attrib.GetProperty("ip").GetString() ?? string.Empty;
                var port = attrib.GetProperty("port").GetInt32();
                var isDefault = attrib.GetProperty("is_default").GetBoolean();
                allocations.Add(new ServerAllocation { Uuid = uuid, Ip = ip, Port = port, IsDefault = isDefault });
            }
        }
        return allocations;
    }

    internal static List<ServerInfo> ExtractServerListInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var serverInfo = new List<ServerInfo>();
        var serversArray = root.GetProperty("data").EnumerateArray();
        foreach (var server in serversArray)
        {
            var attributes = server.GetProperty("attributes");
            var id = attributes.GetProperty("id").GetInt32();
            var uuid = attributes.GetProperty("uuid").GetString() ?? string.Empty;
            var name = attributes.GetProperty("name").GetString() ?? string.Empty;
            var egg = attributes.GetProperty("egg").GetInt32();

            long memoryLimitBytes = 0;
            // Pelican API structure for limits
            if (attributes.TryGetProperty("limits", out var limits))
            {
                if (limits.TryGetProperty("memory", out var memElement))
                {
                    long memMb = memElement.GetInt64();
                    // memMb = 0 means unlimited
                    if (memMb > 0) memoryLimitBytes = memMb * 1024 * 1024;
                }
            }

            serverInfo.Add(new ServerInfo
            {
                Id = id,
                Uuid = uuid,
                Name = name,
                MaxMemoryBytes = memoryLimitBytes,
                Egg = new EggInfo { Id = egg }
            });
        }

        return serverInfo;
    }

    internal static ServerResources ExtractServerResources(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var attributes = root.GetProperty("attributes");
        var resources = attributes.GetProperty("resources");

        var currentState = attributes.GetProperty("current_state").GetString() ?? string.Empty;
        var memory = resources.GetProperty("memory_bytes").GetInt64();
        var cpu = resources.GetProperty("cpu_absolute").GetDouble();
        var disk = resources.GetProperty("disk_bytes").GetInt64();
        var networkRx = resources.GetProperty("network_rx_bytes").GetInt64();
        var networkTx = resources.GetProperty("network_tx_bytes").GetInt64();
        var uptime = resources.GetProperty("uptime").GetInt64();

        return new ServerResources
        {
            CurrentState = currentState,
            MemoryBytes = memory,
            CpuAbsolute = cpu,
            DiskBytes = disk,
            NetworkRxBytes = networkRx,
            NetworkTxBytes = networkTx,
            Uptime = uptime
        };
    }
}