using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using RestSharp;

namespace Pelican_Keeper;

using static TemplateClasses;

public static class HelperClass
{
    private static readonly Dictionary<string, string> LastEmbedHashes = new();
    
    /// <summary>
    /// Creates a rest request to the Pelican API
    /// </summary>
    /// <param name="client">RestClient</param>
    /// <param name="token">Pelican API token</param>
    /// <returns>The RestResponse</returns>
    public static RestResponse CreateRequest(RestClient client, string? token)
    {
        var request = new RestRequest("");
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Authorization", "Bearer " + token);
        var response = client.Execute(request);
        return response;
    }

    /// <summary>
    /// Checks if the embed has changed
    /// </summary>
    /// <param name="uuid">list of server UUIDs</param>
    /// <param name="newEmbed">new embed</param>
    /// <returns>bool whether the embed has changed</returns>
    internal static bool EmbedHasChanged(List<string?> uuid, DiscordEmbed newEmbed)
    {
        foreach (var uuidItem in uuid)
        {
            if (uuidItem == null) continue;
            var hash = newEmbed.Description + string.Join(",", newEmbed.Fields.Select(f => f.Name + f.Value));
            if (LastEmbedHashes.TryGetValue(uuidItem, out var lastHash) && lastHash == hash) return false;
            LastEmbedHashes[uuidItem] = hash;
        }
        return true;
    }

    /// <summary>
    /// Gets the Main connectable IP and Port by checking if the allocation is set as the default.
    /// </summary>
    /// <param name="serverInfo">ServerInfo of the server</param>
    /// <returns>The allocation that's marked as the default</returns>
    private static ServerAllocation? GetConnectableAllocation(ServerInfo serverInfo) //TODO: I need more logic here to determine the best allocation to use and to determine the right port if the main port is not he joining port, for example in ark se its the query port
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
            ConsoleExt.WriteLine("No connectable allocation found for server: " + serverInfo.Name, ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Error);
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
    public static string GetReadableConnectableAddress(ServerInfo serverInfo)
    {
        var allocation = GetConnectableAllocation(serverInfo);
        if (allocation == null)
        {
            ConsoleExt.WriteLine("No connectable allocation found for server: " + serverInfo.Name, ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Error);
            return "No Connectable Address";
        }
        
        return $"{GetCorrectIp(serverInfo)}:{allocation.Port}"; //TODO: Allow for usage of domain names in the future
    }
    
    /// <summary>
    /// Sorts a list of ServerInfo's in the desired format and direction.
    /// </summary>
    /// <param name="servers">List of ServerInfos</param>
    /// <param name="sortFormat">The Format the Servers should be sorted in</param>
    /// <param name="direction">The direction the Servers should be sorted in</param>
    /// <returns>The Sorted List of ServerInfo's</returns>
    public static List<ServerInfo> SortServers(IEnumerable<ServerInfo> servers, MessageSorting sortFormat, MessageSortingDirection direction)
    {
        return (field: sortFormat, direction) switch
        {
            (MessageSorting.Name, MessageSortingDirection.Ascending) => servers.OrderBy(s => s.Name).ToList(),
            (MessageSorting.Name, MessageSortingDirection.Descending) => servers.OrderByDescending(s => s.Name).ToList(),
            (MessageSorting.Status, MessageSortingDirection.Ascending) => servers.OrderBy(s => s.Resources?.CurrentState).ToList(),
            (MessageSorting.Status, MessageSortingDirection.Descending) => servers.OrderByDescending(s => s.Resources?.CurrentState).ToList(),
            (MessageSorting.Uptime, MessageSortingDirection.Ascending) => servers.OrderBy(s => s.Resources?.Uptime).ToList(),
            (MessageSorting.Uptime, MessageSortingDirection.Descending) => servers.OrderByDescending(s => s.Resources?.Uptime).ToList(),
            _ => servers.ToList()
        };
    }

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
                if (Program.Config.Debug)
                    ConsoleExt.WriteLine($"Player count returned by Custom Regex: {count}", ConsoleExt.CurrentStep.Helper);
                return count;
            }
        }
        
        ConsoleExt.WriteLine("The Bot was unable to determine the Player Count of the Server!", ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Error, new Exception(serverResponse));
        return 0;
    }

    /// <summary>
    /// Cleans up the Server response into a clean and readable end user display string.
    /// </summary>
    /// <param name="serverResponse">The Server response</param>
    /// <param name="maxPlayers">Optional! A hard-coded string if left empty for the max number the server can have</param>
    /// <returns>A User readable string of the player count</returns>
    public static string ServerPlayerCountDisplayCleanup(string? serverResponse, int maxPlayers = 0)
    {
        string maxPlayerCount = "Unknown";
        
        if (string.IsNullOrEmpty(serverResponse) && maxPlayers > 0)
        {
            return $"N/A/{maxPlayers}";
        }
        
        if (string.IsNullOrEmpty(serverResponse))
        {
            return "N/A";
        }

        if (maxPlayers != 0)
        {
            maxPlayerCount = maxPlayers.ToString();
        }

        return $"{serverResponse}/{maxPlayerCount}";
    }
    
    /// <summary>
    /// Chunks any list of items into multiple lists with the desired size
    /// </summary>
    /// <param name="source">Source List</param>
    /// <param name="size">Maximum size you want the output lists to be</param>
    /// <typeparam name="T">Any Type</typeparam>
    /// <returns>A List of Lists with the original items being chunked</returns>
    public static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> source, int size)
    {
        var list = new List<T>(size);
        foreach (var item in source)
        {
            list.Add(item);
            if (list.Count != size) continue;
            yield return list;
            list.Clear();
        }
        if (list.Count > 0) yield return list;
    }
    
    /// <summary>
    /// Puts a list of DiscordComponent's each into one line
    /// </summary>
    /// <param name="mb">DiscordMessageBuilder</param>
    /// <param name="components">List of DiscordComponent's</param>
    /// <param name="maxRows">Maximum number of rows you allow, Default 5</param>
    public static void AddRows(this DiscordMessageBuilder mb, IEnumerable<DiscordComponent> components, int maxRows = 5)
    {
        var rowsUsed = 0;
        var buttonBuffer = new List<DiscordComponent>(capacity: 5);

        void FlushButtons()
        {
            if (buttonBuffer.Count == 0) return;
            // pack buttons in rows of up to 5
            foreach (var chunk in buttonBuffer.Chunk(5))
            {
                if (rowsUsed >= maxRows) return;
                mb.AddComponents(chunk);
                rowsUsed++;
            }
            buttonBuffer.Clear();
        }

        foreach (var comp in components)
        {
            switch (comp)
            {
                case DiscordSelectComponent select:
                    FlushButtons();
                    if (rowsUsed >= maxRows) return;
                    // a select must be the only item in its row otherwise discord will freak out for some weird reason
                    mb.AddComponents(select);
                    rowsUsed++;
                    break;

                default:
                    // everything else gets treated as a button-like component
                    buttonBuffer.Add(comp);
                    // if it accumulated 5, it will flush a row
                    if (buttonBuffer.Count == 5)
                        FlushButtons();
                    break;
            }

            if (rowsUsed >= maxRows) break;
        }

        // flush any remaining buttons at the end
        if (rowsUsed < maxRows)
            FlushButtons();
    }
    
    /// <summary>
    /// A Console Dump for a list of DiscordComponent's and their contents
    /// </summary>
    /// <param name="comps">List of DiscordComponent's</param>
    public static void DebugDumpComponents(IEnumerable<DiscordComponent> comps)
    {
        int rows = 0, total = 0;
        var buffer = new List<DiscordComponent>(5);

        void FlushButtons()
        {
            if (buffer.Count == 0) return;
            rows++;
            Console.WriteLine($"[ROW {rows}] {buffer.Count} button(s)");
            foreach (var b in buffer)
            {
                if (b is DiscordButtonComponent btn)
                {
                    Console.WriteLine($"  • Button: style={btn.Style}, label='{btn.Label}' len={btn.Label?.Length ?? 0}, custom_id='{btn.CustomId}' len={btn.CustomId?.Length ?? 0}, type='{btn.Type}'");
                    total++;
                }
            }
            buffer.Clear();
        }

        foreach (var c in comps)
        {
            switch (c)
            {
                case DiscordSelectComponent s:
                    // Buttons row before a select
                    FlushButtons();
                    rows++;
                    Console.WriteLine($"[ROW {rows}] 1 select: custom_id='{s.CustomId}', placeholder='{s.Placeholder}' len={s.Placeholder?.Length ?? 0}, options={s.Options?.Count}");
                    if (s.Options != null)
                    {
                        for (int i = 0; i < s.Options.Count; i++)
                        {
                            var o = s.Options[i];
                            Console.WriteLine($"    - opt[{i}]: label='{o.Label}' len={o.Label.Length}, value='{o.Value}' len={o.Value.Length}, desc len={o.Description?.Length ?? 0}");
                            total++;
                        }
                    }
                    break;

                case DiscordButtonComponent b:
                    buffer.Add(b);
                    if (buffer.Count == 5) FlushButtons();
                    break;

                default:
                    Console.WriteLine($"[WARN] Unknown component type: {c.GetType().Name}");
                    break;
            }
        }
        FlushButtons();
        Console.WriteLine($"[SUMMARY] rows={rows}, total components={total}");
    }
    
    /// <summary>
    /// Gets the raw JSON text from a URL
    /// </summary>
    /// <param name="url">Github URL</param>
    /// <returns>the raw JSON</returns>
    public static async Task<string> GetJsonTextAsync(string url)
    {
        using var http = new HttpClient();
        return await http.GetStringAsync(url);
    }
}