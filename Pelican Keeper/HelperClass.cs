using System.Globalization;
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
                ConsoleExt.WriteLine($"Player count returned by Custom Regex: {count}", ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Debug);
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
            ConsoleExt.WriteLine($"[ROW {rows}] {buffer.Count} button(s)", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
            foreach (var b in buffer)
            {
                if (b is DiscordButtonComponent btn)
                {
                    ConsoleExt.WriteLine($"  • Button: style={btn.Style}, label='{btn.Label}' len={btn.Label?.Length ?? 0}, custom_id='{btn.CustomId}' len={btn.CustomId?.Length ?? 0}, type='{btn.Type}'", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
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
                    ConsoleExt.WriteLine($"[ROW {rows}] 1 select: custom_id='{s.CustomId}', placeholder='{s.Placeholder}' len={s.Placeholder?.Length ?? 0}, options={s.Options?.Count}", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
                    if (s.Options != null)
                    {
                        for (int i = 0; i < s.Options.Count; i++)
                        {
                            var o = s.Options[i];
                            ConsoleExt.WriteLine($"    - opt[{i}]: label='{o.Label}' len={o.Label.Length}, value='{o.Value}' len={o.Value.Length}, desc len={o.Description?.Length ?? 0}", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
                            total++;
                        }
                    }
                    break;

                case DiscordButtonComponent b:
                    buffer.Add(b);
                    if (buffer.Count == 5) FlushButtons();
                    break;

                default:
                    ConsoleExt.WriteLine($"[WARN] Unknown component type: {c.GetType().Name}", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
                    break;
            }
        }
        FlushButtons();
        ConsoleExt.WriteLine($"[SUMMARY] rows={rows}, total components={total}", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
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

    /// <summary>
    /// Takes an input string number and checks if its zero, null or empty
    /// </summary>
    /// <param name="value">input string number</param>
    /// <returns>null if null of empty, or Infinite if 0, otherwise its Original value</returns>
    public static string IfZeroThenInfinite(string value)
    {
        return String.IsNullOrEmpty(value) ? "null" : value == "0" ? "∞" : value;
    }

    /// <summary>
    /// Adds a % at the end if it's a number
    /// </summary>
    /// <param name="value">Input String</param>
    /// <returns>Original Value if ∞ or the word null, or number with % at the end if a number</returns>
    public static string DynamicallyAddPercentSign(string value)
    { 
        return value is "∞" or "null"
            ? value
            : double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                ? $"{value}%"
                : value;
    }

    /// <summary>
    /// Checks the size of each individual component of the embed and compares it to its max size for each component.
    /// </summary>
    /// <param name="embed">Discord Embed</param>
    /// <returns>True if Size Passes and false if it doesn't</returns>
    public static bool EmbedLengthCheck(DiscordEmbed embed)
    {
        // Per-embed limits in characters
        // Title: 256
        // Description: 4096
        // Field name: 256
        // Field value: 1024
        // Footer text: 2048
        // Author name: 256
        // Total embed characters: 6000
        
        bool sizePasses = true;
        int fullSize = 0;

        if (embed.Title is { Length: > 256 })
        {
            sizePasses = false;
            fullSize += embed.Title.Length;
            ConsoleExt.WriteLine($"Title length: {embed.Title.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        }
        
        if (embed.Description is { Length: > 4096 })
        {
            sizePasses = false;
            fullSize += embed.Description.Length;
            ConsoleExt.WriteLine($"Description length: {embed.Description.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        }
        
        foreach (var field in embed.Fields)
        {
            if (field.Name is { Length: > 256 })
            {
                sizePasses = false;
                fullSize += field.Name.Length;
                ConsoleExt.WriteLine($"Field's {field.Name} Name length: {field.Name.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
            }
            if  (field.Value is { Length: > 1024 })
            {
                sizePasses = false;
                fullSize += field.Value.Length;
                ConsoleExt.WriteLine($"Field's {field.Name} Value length: {field.Value.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
            }
        }

        if (embed.Footer?.Text is { Length: > 2048 })
        {
            sizePasses = false;
            fullSize += embed.Footer.Text.Length;
            ConsoleExt.WriteLine($"Footer Text length: {embed.Footer.Text.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        }

        if (embed.Author?.Name is { Length: > 256 })
        {
            sizePasses = false;
            fullSize += embed.Author.Name.Length;
            ConsoleExt.WriteLine($"Author Name length: {embed.Author.Name.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        }

        if (fullSize > 6000)
        {
            sizePasses = false;
            ConsoleExt.WriteLine($"Full Size Embed length: {fullSize}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        }
        
        return sizePasses;
    }
}