namespace Pelican_Keeper.Utilities;

/// <summary>
/// Formatting utilities for byte sizes and time durations.
/// </summary>
public static class FormatHelper
{
    /// <summary>
    /// Formats byte count to human-readable string (KB, MB, GB, TB).
    /// Uses decimal units (1000-based) to match Pelican Panel's display.
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        // Pelican uses SI/decimal units (1000-based) throughout
        const long kb = 1000;
        const long mb = 1000 * 1000;              // 1,000,000 bytes = 1 MB
        const long gb = 1000 * 1000 * 1000;       // 1,000,000,000 bytes = 1 GB
        const long tb = 1000L * 1000 * 1000 * 1000; // 1,000,000,000,000 bytes = 1 TB

        return bytes switch
        {
            >= tb => $"{bytes / (double)tb:F2} TB",
            >= gb => $"{bytes / (double)gb:F2} GB",
            >= mb => $"{bytes / (double)mb:F2} MB",
            >= kb => $"{bytes / (double)kb:F2} KB",
            _ => $"{bytes} B"
        };
    }

    /// <summary>
    /// Formats uptime in milliseconds to "Xd Xh Xm" format.
    /// </summary>
    public static string FormatUptime(long uptimeMs)
    {
        var uptime = TimeSpan.FromMilliseconds(uptimeMs);
        return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
    }

    /// <summary>
    /// Returns emoji icon for server status.
    /// </summary>
    public static string GetStatusIcon(string status) => status.ToLower() switch
    {
        "offline" => "🔴",
        "missing" => "🟡",
        "running" => "🟢",
        _ => "⚪"
    };
}
