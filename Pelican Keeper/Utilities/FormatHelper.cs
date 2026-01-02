namespace Pelican_Keeper.Utilities;

/// <summary>
/// Formatting utilities for byte sizes and time durations.
/// </summary>
public static class FormatHelper
{
    /// <summary>
    /// Formats byte count to human-readable string (KB, MB, GB, TB).
    /// Uses 1024 for byte-to-MB conversion (matching Pelican's internal storage),
    /// but 1000 for MB-to-GB display (so 4000 MB = 4 GB as users expect).
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        // Pelican stores memory as MiB internally (1024-based)
        const long kb = 1024;
        const long mb = kb * 1024;           // 1,048,576 bytes = 1 MB (matching Pelican)
        const long gb = mb * 1000;           // 1000 MB = 1 GB (user expectation)
        const long tb = gb * 1000;           // 1000 GB = 1 TB

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
