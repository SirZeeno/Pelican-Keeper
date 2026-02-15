using DSharpPlus.Entities;
using RestSharp;

namespace Pelican_Keeper.Helper_Classes;

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