using System.Text.RegularExpressions;

namespace Pelican_Keeper.Helper_Classes;

using static TemplateClasses;

public static class ExtractorHelpers
{
    /// <summary>
    /// Extracts the Player count for a server depending on its response.
    /// </summary>
    /// <param name="serverResponse">The Servers Response</param>
    /// <param name="regexPattern">Optional! Custom Regex Pattern provided by the user</param>
    /// <returns>An int of the Player count, and 0 if nothing is found</returns>
    public static int ExtractPlayerCount(string? serverResponse, string? regexPattern = null)
    {
        if (string.IsNullOrEmpty(serverResponse))
        {
            ConsoleExt.WriteLine("The Response of the Server was Empty or Null!", ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Error);
            return 0;
        }

        if (!serverResponse.Any(char.IsDigit))
        {
            return 0; // No digits found in the server response, so player count is 0 (probably a "no players" message or connection error/timeout)
        }
        
        var playerMaxPlayer = Regex.Match(serverResponse, @"^(\d+)\/\d+$");
        if (playerMaxPlayer.Success && int.TryParse(playerMaxPlayer.Groups[1].Value, out var playerCount))
        {
            ConsoleExt.WriteLine($"Player count extracted using standard format: {playerCount}", ConsoleExt.CurrentStep.Helper);
            return playerCount;
        }
        
        var arkRconPlayerList = Regex.Matches(serverResponse, @"(\d+)\.\s*([^,]+),\s*(.+)$", RegexOptions.Multiline);
        if (arkRconPlayerList.Count > 0)
        {
            ConsoleExt.WriteLine($"Player count extracted using Ark format: {arkRconPlayerList.Count}", ConsoleExt.CurrentStep.Helper);
            return arkRconPlayerList.Count;
        }

        var palworldPlayerList = Regex.Match(serverResponse, @"^(?!name,).+$", RegexOptions.Multiline);
        if (palworldPlayerList.Success && serverResponse.Contains("name,playeruid,steamid"))
        {
            ConsoleExt.WriteLine($"Player count extracted using Palworld format: {palworldPlayerList.Length}", ConsoleExt.CurrentStep.Helper);
            return palworldPlayerList.Length;
        }
        
        var factorioPlayerList = Regex.Match(serverResponse, @"Online players \((\d+)\):");
        if (factorioPlayerList.Success && int.TryParse(factorioPlayerList.Groups[1].Value, out var factorioPlayerCount))
        {
            ConsoleExt.WriteLine($"Player count extracted using Factorio format: {factorioPlayerCount}", ConsoleExt.CurrentStep.Helper);
            return factorioPlayerCount;
        }
        
        // Custom User-defined regex pattern
        if (regexPattern != null)
        {
            var customMatch = Regex.Match(serverResponse, regexPattern);
            if (customMatch.Success)
            {
                if (!Int32.TryParse(customMatch.Value, out var count)) return count;
                ConsoleExt.WriteLine($"Player count returned by Custom Regex: {count}", ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Debug);
                return count;
            }
        }
        
        ConsoleExt.WriteLine("The Bot was unable to determine the Player Count of the Server!", ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Error, new Exception(serverResponse));
        return 0;
    }
    
    /// <summary>
    /// Gets the Main connectable IP and Port by checking if the allocation is set as the default.
    /// </summary>
    /// <param name="serverInfo">ServerInfo of the server</param>
    /// <returns>The allocation that's marked as the default</returns>
    private static ServerAllocation? GetConnectableAllocation(ServerInfo serverInfo) //TODO: I need more logic here to determine the best allocation to use and to determine the right port if the main port is not the joining port, for example in ark se its the query port
    {
        if (serverInfo.Allocations == null || serverInfo.Allocations.Count == 0)
            ConsoleExt.WriteLine("Empty allocations for server: " + serverInfo.Name, ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Warning);
        return serverInfo.Allocations?.FirstOrDefault(allocation => allocation.IsDefault) ?? serverInfo.Allocations?.FirstOrDefault();
    }
 
    /// <summary>
    /// Determines if the IP is Internal or External and returns the Internal one if it's Internal and the Secrets specified External one if it doesn't match the Internal structure.
    /// </summary>
    /// <param name="serverInfo">ServerInfo of the Server</param>
    /// <returns>Internal or External IP</returns>
    public static string GetCorrectIp(ServerInfo serverInfo)
    {
        var allocation = GetConnectableAllocation(serverInfo);
        if (allocation == null)
        {
            ConsoleExt.WriteLine("[GetCorrectIp] No connectable allocation found for server: " + serverInfo.Name, ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Error);
            return "No Connectable Address";
        }
        
        if (Program.Config.InternalIpStructure != null)
        {
            string internalIpPattern = "^" + Regex.Escape(Program.Config.InternalIpStructure).Replace("\\*", "\\d+") + "$";
            if (Regex.Match(allocation.Ip, internalIpPattern) is { Success: true })
            {
                return allocation.Ip;
            }
        }

        return Program.Secrets.ExternalServerIp ?? "0.0.0.0";
    }
    
    /// <summary>
    /// Puts the ServerAllocation of a ServerInfo into a readable string format for the end user
    /// </summary>
    /// <param name="serverInfo">ServerInfo of the server</param>
    /// <returns>No Connectable Address if nothing is found, and Ip:Port if a match is found</returns>
    public static string GetReadableConnectableAddress(ServerInfo serverInfo) //TODO: mat-pandaz was having issues here when it came to his servers not displaying the connectable IP
    {
        var allocation = GetConnectableAllocation(serverInfo);
        if (allocation == null)
        {
            ConsoleExt.WriteLine("[GetReadableConnectableAddress] No connectable allocation found for server: " + serverInfo.Name, ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Error);
            return "No Connectable Address";
        }
        
        return $"{GetCorrectIp(serverInfo)}:{allocation.Port}"; //TODO: Allow for usage of domain names in the future
    }
}