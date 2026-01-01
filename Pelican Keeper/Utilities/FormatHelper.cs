namespace Pelican_Keeper.Utilities;

/// <summary>
/// Formatting utilities for byte sizes and time durations.
/// </summary>
public static class FormatHelper
{
    /// <summary>
    /// Formats byte count to human-readable string (KB, MB, GB, TB).
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        const long kb = 1000;
        const long mb = kb * 1000;
        const long gb = mb * 1000;
        const long tb = gb * 1000;

        return bytes switch
        {
            >= tb => $"{bytes / (double)tb:F2} TB",
            >= gb => $"{bytes / (double)gb:F2} GB",
            >= mb => $"{bytes / (double)mb:F2} MB",
            >= kb => $"{bytes / (double)kb:F2} kB",
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
