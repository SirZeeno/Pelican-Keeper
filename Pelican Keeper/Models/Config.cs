namespace Pelican_Keeper.Models;

/// <summary>
/// Application configuration controlling display, monitoring, and behavior.
/// Values can be overridden by environment variables.
/// </summary>
public class Config
{
    /// <summary>IP pattern for identifying internal network addresses (e.g., "192.168.*.*").</summary>
    public string? InternalIpStructure { get; set; }

    /// <summary>Display format for server status messages.</summary>
    public MessageFormat MessageFormat { get; set; } = MessageFormat.None;

    /// <summary>Field used for sorting the server list.</summary>
    public MessageSorting MessageSorting { get; set; } = MessageSorting.None;

    /// <summary>Sort direction for the server list.</summary>
    public MessageSortingDirection MessageSortingDirection { get; set; } = MessageSortingDirection.None;

    /// <summary>Hide offline servers from the display.</summary>
    public bool IgnoreOfflineServers { get; set; }

    /// <summary>Hide servers with internal IP addresses.</summary>
    public bool IgnoreInternalServers { get; set; }

    /// <summary>Server UUIDs to exclude from display.</summary>
    public string[]? ServersToIgnore { get; set; }

    /// <summary>Show joinable IP:Port for each server.</summary>
    public bool JoinableIpDisplay { get; set; }

    /// <summary>Query and display player counts.</summary>
    public bool PlayerCountDisplay { get; set; }

    /// <summary>Server UUIDs to monitor for player counts.</summary>
    public string[]? ServersToMonitor { get; set; }

    /// <summary>Enable automatic shutdown of empty servers.</summary>
    public bool AutomaticShutdown { get; set; }

    /// <summary>Server UUIDs eligible for automatic shutdown.</summary>
    public string[]? ServersToAutoShutdown { get; set; }

    /// <summary>Time format "d:hh:mm" before shutting down empty servers.</summary>
    public string? EmptyServerTimeout { get; set; }

    /// <summary>Allow Discord users to start servers via buttons/dropdowns.</summary>
    public bool AllowUserServerStartup { get; set; }

    /// <summary>Server UUIDs that can be started by users.</summary>
    public string[]? AllowServerStartup { get; set; }

    /// <summary>Discord user IDs allowed to start servers.</summary>
    public string[]? UsersAllowedToStartServers { get; set; }

    /// <summary>Allow Discord users to stop servers via buttons/dropdowns.</summary>
    public bool AllowUserServerStopping { get; set; }

    /// <summary>Server UUIDs that can be stopped by users.</summary>
    public string[]? AllowServerStopping { get; set; }

    /// <summary>Discord user IDs allowed to stop servers.</summary>
    public string[]? UsersAllowedToStopServers { get; set; }

    /// <summary>Continuously reload MessageMarkdown.txt during runtime.</summary>
    public bool ContinuesMarkdownRead { get; set; }

    /// <summary>Continuously reload GamesToMonitor.json during runtime.</summary>
    public bool ContinuesGamesToMonitorRead { get; set; }

    /// <summary>Interval in seconds for reloading markdown template.</summary>
    public int MarkdownUpdateInterval { get; set; }

    private int _serverUpdateInterval;
    /// <summary>Interval in seconds between Pelican API polls. Minimum 10.</summary>
    public int ServerUpdateInterval
    {
        get => _serverUpdateInterval;
        set => _serverUpdateInterval = Math.Max(value, 10);
    }

    /// <summary>Limit the number of servers shown.</summary>
    public bool LimitServerCount { get; set; }

    /// <summary>Maximum servers to display when LimitServerCount is true.</summary>
    public int MaxServerCount { get; set; }

    /// <summary>Specific server UUIDs to display when limiting.</summary>
    public string[]? ServersToDisplay { get; set; }

    /// <summary>Enable verbose console logging.</summary>
    public bool Debug { get; set; }

    /// <summary>Log without sending Discord messages.</summary>
    public bool DryRun { get; set; }

    /// <summary>Automatically download and apply updates on startup.</summary>
    public bool AutoUpdate { get; set; }

    /// <summary>Notify in Discord when an update is available.</summary>
    public bool NotifyOnUpdate { get; set; } = true;
}
