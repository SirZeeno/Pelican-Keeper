using System.Text.RegularExpressions;
using Pelican_Keeper.Core;
using Pelican_Keeper.Models;

namespace Pelican_Keeper.Utilities;

/// <summary>
/// Network and IP address utilities.
/// </summary>
public static class NetworkHelper
{
    /// <summary>
    /// Gets the default (primary) allocation for a server.
    /// </summary>
    public static ServerAllocation? GetDefaultAllocation(ServerInfo serverInfo)
    {
        if (serverInfo.Allocations == null || serverInfo.Allocations.Count == 0)
        {
            Logger.WriteLineWithStep($"Empty allocations for server: {serverInfo.Name}", Logger.Step.Helper, Logger.OutputType.Warning);
            return null;
        }

        return serverInfo.Allocations.FirstOrDefault(a => a.IsDefault) ?? serverInfo.Allocations.FirstOrDefault();
    }

    /// <summary>
    /// Determines the correct IP to display (internal or external).
    /// </summary>
    public static string GetDisplayIp(ServerInfo serverInfo)
    {
        var allocation = GetDefaultAllocation(serverInfo);
        if (allocation == null)
        {
            Logger.WriteLineWithStep($"No allocation found for server: {serverInfo.Name}", Logger.Step.Helper, Logger.OutputType.Error);
            return "No Connectable Address";
        }

        if (!string.IsNullOrEmpty(RuntimeContext.Config.InternalIpStructure))
        {
            var pattern = "^" + Regex.Escape(RuntimeContext.Config.InternalIpStructure).Replace("\\*", "\\d+") + "$";
            if (Regex.IsMatch(allocation.Ip, pattern))
                return allocation.Ip;
        }

        return RuntimeContext.Secrets.ExternalServerIp ?? "0.0.0.0";
    }

    /// <summary>
    /// Gets formatted IP:Port string for display.
    /// </summary>
    public static string GetConnectAddress(ServerInfo serverInfo)
    {
        var allocation = GetDefaultAllocation(serverInfo);
        if (allocation == null)
        {
            Logger.WriteLineWithStep($"No allocation found for server: {serverInfo.Name}", Logger.Step.Helper, Logger.OutputType.Error);
            return "No Connectable Address";
        }

        return $"{GetDisplayIp(serverInfo)}:{allocation.Port}";
    }

    /// <summary>
    /// Checks if an IP matches the internal network pattern.
    /// </summary>
    public static bool IsInternalIp(string ip)
    {
        if (string.IsNullOrEmpty(RuntimeContext.Config.InternalIpStructure))
            return false;

        var pattern = "^" + Regex.Escape(RuntimeContext.Config.InternalIpStructure).Replace("\\*", "\\d+") + "$";
        return Regex.IsMatch(ip, pattern);
    }
}
