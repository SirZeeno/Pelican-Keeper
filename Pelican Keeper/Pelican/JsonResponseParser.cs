using System.Text.Json;
using System.Text.RegularExpressions;
using Pelican_Keeper.Models;

namespace Pelican_Keeper.Pelican;

/// <summary>
/// Parses JSON responses from the Pelican API.
/// </summary>
public static class JsonResponseParser
{
    /// <summary>
    /// Extracts egg information from the eggs API response.
    /// </summary>
    public static List<EggInfo> ExtractEggInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var eggs = new List<EggInfo>();

        foreach (var egg in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var attr = egg.GetProperty("attributes");
            eggs.Add(new EggInfo
            {
                Id = attr.GetProperty("id").GetInt32(),
                Name = attr.GetProperty("name").GetString() ?? ""
            });
        }

        return eggs;
    }

    /// <summary>
    /// Extracts server resource metrics from the resources API response.
    /// </summary>
    public static ServerResources ExtractServerResources(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var attributes = doc.RootElement.GetProperty("attributes");
        var resources = attributes.GetProperty("resources");

        return new ServerResources
        {
            CurrentState = attributes.GetProperty("current_state").GetString() ?? "",
            MemoryBytes = resources.GetProperty("memory_bytes").GetInt64(),
            CpuAbsolute = resources.GetProperty("cpu_absolute").GetDouble(),
            DiskBytes = resources.GetProperty("disk_bytes").GetInt64(),
            NetworkRxBytes = resources.GetProperty("network_rx_bytes").GetInt64(),
            NetworkTxBytes = resources.GetProperty("network_tx_bytes").GetInt64(),
            Uptime = resources.GetProperty("uptime").GetInt64()
        };
    }

    /// <summary>
    /// Extracts server list from the application servers API response.
    /// </summary>
    public static List<ServerInfo> ExtractServerListInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var servers = new List<ServerInfo>();

        foreach (var server in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var attr = server.GetProperty("attributes");
            long memoryLimitBytes = 0;

            if (attr.TryGetProperty("limits", out var limits) &&
                limits.TryGetProperty("memory", out var memElement))
            {
                var memMb = memElement.GetInt64();
                // Pelican uses SI/decimal units: 1 MB = 1,000,000 bytes
                if (memMb > 0) memoryLimitBytes = memMb * 1000 * 1000;
            }

            servers.Add(new ServerInfo
            {
                Id = attr.GetProperty("id").GetInt32(),
                Uuid = attr.GetProperty("uuid").GetString() ?? "",
                Name = attr.GetProperty("name").GetString() ?? "",
                MaxMemoryBytes = memoryLimitBytes,
                Egg = new EggInfo { Id = attr.GetProperty("egg").GetInt32() }
            });
        }

        return servers;
    }

    /// <summary>
    /// Extracts network allocations from the client API response.
    /// </summary>
    public static List<ServerAllocation> ExtractNetworkAllocations(string json, string? serverUuid = null)
    {
        using var doc = JsonDocument.Parse(json);
        var allocations = new List<ServerAllocation>();

        foreach (var data in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var attr = data.GetProperty("attributes");
            var uuid = attr.GetProperty("uuid").GetString() ?? "";

            if (serverUuid != null && uuid != serverUuid) continue;

            foreach (var alloc in attr.GetProperty("relationships")
                         .GetProperty("allocations")
                         .GetProperty("data")
                         .EnumerateArray())
            {
                var allocAttr = alloc.GetProperty("attributes");
                allocations.Add(new ServerAllocation
                {
                    Uuid = uuid,
                    Ip = allocAttr.GetProperty("ip").GetString() ?? "",
                    Port = allocAttr.GetProperty("port").GetInt32(),
                    IsDefault = allocAttr.GetProperty("is_default").GetBoolean()
                });
            }
        }

        return allocations;
    }

    /// <summary>
    /// Extracts RCON port from server variables.
    /// </summary>
    public static int ExtractRconPort(string json, string uuid, string? variableName)
    {
        return ExtractPortVariable(json, uuid, variableName, "RCON_PORT");
    }

    /// <summary>
    /// Extracts query port from server variables.
    /// </summary>
    public static int ExtractQueryPort(string json, string uuid, string? variableName)
    {
        return ExtractPortVariable(json, uuid, variableName, "QUERY_PORT");
    }

    private static int ExtractPortVariable(string json, string uuid, string? variableName, string defaultVar)
    {
        if (variableName != null)
        {
            var match = Regex.Match(variableName, @"SERVER_PORT\s*\+\s*(\d+)");
            if (match.Success)
            {
                var addition = Convert.ToInt32(match.Groups[1].Value);
                var allocations = ExtractNetworkAllocations(json, uuid);
                var defaultAlloc = allocations.Find(x => x.IsDefault);
                return defaultAlloc?.Port + addition ?? 0;
            }

            if (variableName == "SERVER_PORT")
            {
                var allocations = ExtractNetworkAllocations(json, uuid);
                return allocations.Find(x => x.IsDefault)?.Port ?? 0;
            }
        }

        variableName ??= defaultVar;
        if (string.IsNullOrWhiteSpace(variableName)) variableName = defaultVar;

        return ExtractServerVariable<int>(json, uuid, variableName);
    }

    /// <summary>
    /// Extracts RCON password from server variables.
    /// </summary>
    public static string ExtractRconPassword(string json, string uuid, string? variableName)
    {
        variableName ??= "RCON_PASS";
        if (string.IsNullOrWhiteSpace(variableName)) variableName = "RCON_PASS";

        return ExtractServerVariable<string>(json, uuid, variableName) ?? "";
    }

    /// <summary>
    /// Extracts max player count from server variables.
    /// </summary>
    public static int ExtractMaxPlayerCount(string json, string uuid, string? variableName, string? staticValue)
    {
        if (!string.IsNullOrEmpty(staticValue) && int.TryParse(staticValue, out var staticMax))
            return staticMax;

        variableName ??= "MAX_PLAYERS";
        if (string.IsNullOrWhiteSpace(variableName)) variableName = "MAX_PLAYERS";

        return ExtractServerVariable<int>(json, uuid, variableName);
    }

    private static T? ExtractServerVariable<T>(string json, string uuid, string variableName)
    {
        using var doc = JsonDocument.Parse(json);

        foreach (var data in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var serverUuid = data.GetProperty("attributes").GetProperty("uuid").GetString();
            if (serverUuid != uuid) continue;

            var variables = data.GetProperty("attributes")
                .GetProperty("relationships")
                .GetProperty("variables")
                .GetProperty("data");

            foreach (var variable in variables.EnumerateArray())
            {
                var attr = variable.GetProperty("attributes");
                if (attr.GetProperty("env_variable").GetString() != variableName) continue;

                var value = attr.GetProperty("server_value").GetString();
                if (string.IsNullOrEmpty(value)) return default;

                if (typeof(T) == typeof(int) && int.TryParse(value, out var intVal))
                    return (T)(object)intVal;

                if (typeof(T) == typeof(string))
                    return (T)(object)value;
            }
        }

        return default;
    }
}
