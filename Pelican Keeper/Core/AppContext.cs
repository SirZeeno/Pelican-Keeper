using DSharpPlus.Entities;
using Pelican_Keeper.Models;

namespace Pelican_Keeper.Core;

/// <summary>
/// Centralized runtime state for the application.
/// Replaces scattered static globals with a single point of access.
/// </summary>
public static class RuntimeContext
{
    /// <summary>
    /// Discord channels where the bot posts server status messages.
    /// </summary>
    public static List<DiscordChannel> TargetChannels { get; set; } = [];

    /// <summary>
    /// Discord channel for update notifications. Falls back to first TargetChannel if not set.
    /// </summary>
    public static DiscordChannel? NotificationChannel { get; set; }

    /// <summary>
    /// API credentials and connection settings.
    /// </summary>
    public static Secrets Secrets { get; set; } = null!;

    /// <summary>
    /// Application configuration loaded from Config.json and environment variables.
    /// </summary>
    public static Config Config { get; set; } = null!;

    /// <summary>
    /// Current embed pages for paginated display mode.
    /// </summary>
    public static List<DiscordEmbed> EmbedPages { get; set; } = [];

    /// <summary>
    /// Cached server information from the latest Pelican API fetch.
    /// </summary>
    public static List<ServerInfo> ServerInfoCache { get; set; } = [];

    /// <summary>
    /// Application version from assembly metadata.
    /// </summary>
    public static string Version => typeof(RuntimeContext).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>
    /// Resets all state. Primarily used for testing.
    /// </summary>
    public static void Reset()
    {
        TargetChannels = [];
        Secrets = null!;
        Config = null!;
        EmbedPages = [];
        ServerInfoCache = [];
    }
}
