namespace Pelican_Keeper.Utilities;

/// <summary>
/// Formatting utilities for byte sizes and time durations.
/// </summary>
public static class FormatHelper
{
    /// <summary>
    /// Formats byte count to human-readable string (KiB, MiB, GiB, TiB).
    /// Uses binary units (1024-based) which matches how memory is typically measured.
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        const long kib = 1024;
        const long mib = kib * 1024;
        const long gib = mib * 1024;
        const long tib = gib * 1024;

        return bytes switch
        {
            >= tib => $"{bytes / (double)tib:F2} TB",
            >= gib => $"{bytes / (double)gib:F2} GB",
            >= mib => $"{bytes / (double)mib:F2} MB",
            >= kib => $"{bytes / (double)kib:F2} KB",
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
