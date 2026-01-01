namespace Pelican_Keeper.Models;

/// <summary>
/// Server information retrieved from Pelican application API.
/// </summary>
public class ServerInfo
{
    /// <summary>Pelican internal server ID.</summary>
    public int Id { get; init; }

    /// <summary>Unique server identifier.</summary>
    public string Uuid { get; init; } = null!;

    /// <summary>Display name of the server.</summary>
    public string Name { get; init; } = null!;

    /// <summary>Egg (game type) information.</summary>
    public EggInfo Egg { get; init; } = null!;

    /// <summary>Maximum memory allocation in bytes.</summary>
    public long MaxMemoryBytes { get; set; }

    /// <summary>Live resource usage from client API.</summary>
    public ServerResources? Resources { get; set; }

    /// <summary>Network allocations (IP/port bindings).</summary>
    public List<ServerAllocation>? Allocations { get; set; }

    /// <summary>Formatted player count text for display.</summary>
    public string? PlayerCountText { get; set; }
}

/// <summary>
/// Live server resource metrics from Pelican client API.
/// </summary>
public class ServerResources
{
    /// <summary>Current power state (running, offline, starting, stopping).</summary>
    public string CurrentState { get; init; } = null!;

    /// <summary>Current memory usage in bytes.</summary>
    public long MemoryBytes { get; init; }

    /// <summary>CPU usage percentage.</summary>
    public double CpuAbsolute { get; init; }

    /// <summary>Disk usage in bytes.</summary>
    public long DiskBytes { get; init; }

    /// <summary>Network bytes received.</summary>
    public long NetworkRxBytes { get; init; }

    /// <summary>Network bytes transmitted.</summary>
    public long NetworkTxBytes { get; init; }

    /// <summary>Server uptime in milliseconds.</summary>
    public long Uptime { get; init; }
}

/// <summary>
/// Network allocation (IP/port binding) for a server.
/// </summary>
public class ServerAllocation
{
    /// <summary>Server UUID this allocation belongs to.</summary>
    public string Uuid { get; init; } = null!;

    /// <summary>IP address.</summary>
    public string Ip { get; init; } = null!;

    /// <summary>Port number.</summary>
    public int Port { get; init; }

    /// <summary>Whether this is the primary allocation.</summary>
    public bool IsDefault { get; init; }
}

/// <summary>
/// Pelican egg (game type) metadata.
/// </summary>
public class EggInfo
{
    /// <summary>Egg ID in Pelican.</summary>
    public int Id { get; init; }

    /// <summary>Display name of the egg/game.</summary>
    public string Name { get; set; } = null!;
}
