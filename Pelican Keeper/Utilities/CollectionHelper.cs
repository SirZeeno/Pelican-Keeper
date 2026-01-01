using Pelican_Keeper.Models;

namespace Pelican_Keeper.Utilities;

/// <summary>
/// Collection and sorting utilities.
/// </summary>
public static class CollectionHelper
{
    /// <summary>
    /// Chunks a sequence into groups of specified size.
    /// </summary>
    public static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> source, int size)
    {
        var list = new List<T>(size);
        foreach (var item in source)
        {
            list.Add(item);
            if (list.Count == size)
            {
                yield return new List<T>(list);
                list.Clear();
            }
        }
        if (list.Count > 0) yield return list;
    }

    /// <summary>
    /// Sorts servers by the specified field and direction.
    /// </summary>
    public static List<ServerInfo> SortServers(IEnumerable<ServerInfo> servers, MessageSorting sorting, MessageSortingDirection direction)
    {
        return (sorting, direction) switch
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
}
