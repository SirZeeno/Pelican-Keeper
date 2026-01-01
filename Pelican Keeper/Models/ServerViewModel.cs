namespace Pelican_Keeper.Models;

/// <summary>
/// View model for template rendering with server data.
/// </summary>
public class ServerViewModel
{
    /// <summary>Formatted player count display.</summary>
    public string PlayerCount { get; set; } = null!;

    /// <summary>Joinable IP:Port address.</summary>
    public string IpAndPort { get; set; } = null!;

    /// <summary>Server UUID.</summary>
    public string? Uuid { get; set; }

    /// <summary>Server display name.</summary>
    public string ServerName { get; init; } = null!;

    /// <summary>Current power state text.</summary>
    public string Status { get; set; } = null!;

    /// <summary>Emoji icon representing status.</summary>
    public string StatusIcon { get; set; } = null!;

    /// <summary>Formatted CPU usage.</summary>
    public string Cpu { get; set; } = null!;

    /// <summary>Formatted memory usage.</summary>
    public string Memory { get; set; } = null!;

    /// <summary>Formatted disk usage.</summary>
    public string Disk { get; set; } = null!;

    /// <summary>Formatted network received.</summary>
    public string NetworkRx { get; set; } = null!;

    /// <summary>Formatted network transmitted.</summary>
    public string NetworkTx { get; set; } = null!;

    /// <summary>Formatted uptime string.</summary>
    public string Uptime { get; set; } = null!;
}
