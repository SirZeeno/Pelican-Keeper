using System.Text.Json.Serialization;

namespace Pelican_Keeper.Models;

/// <summary>
/// Discord message display format for server status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageFormat
{
    /// <summary>Individual embed per server.</summary>
    PerServer,
    /// <summary>All servers in a single embed with dropdowns.</summary>
    Consolidated,
    /// <summary>Page-through servers with navigation buttons.</summary>
    Paginated,
    /// <summary>No display (disabled).</summary>
    None
}

/// <summary>
/// Field used for sorting servers in the display.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageSorting
{
    Name,
    Status,
    Uptime,
    None
}

/// <summary>
/// Direction for server list sorting.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageSortingDirection
{
    Ascending,
    Descending,
    None
}

/// <summary>
/// Protocol used to query game servers for player count.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommandExecutionMethod
{
    MinecraftJava,
    MinecraftBedrock,
    Rcon,
    A2S,
    Terraria
}

/// <summary>
/// Server power state from Pelican API.
/// </summary>
public enum ServerStatus
{
    Online,
    Offline,
    Paused,
    Starting,
    Stopping
}
