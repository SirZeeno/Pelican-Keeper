namespace Pelican_Keeper.Models;

/// <summary>
/// Persistent storage for tracking Discord message IDs.
/// </summary>
public class LiveMessageJsonStorage
{
    /// <summary>Message IDs for non-paginated displays.</summary>
    public HashSet<ulong>? LiveStore { get; set; } = [];

    /// <summary>Message ID to page index mapping for paginated displays.</summary>
    public Dictionary<ulong, int>? PaginatedLiveStore { get; set; } = [];
}
