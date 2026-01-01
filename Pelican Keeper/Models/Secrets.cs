namespace Pelican_Keeper.Models;

/// <summary>
/// API credentials and connection settings for Pelican and Discord.
/// </summary>
/// <param name="ClientToken">Pelican client API token for server resource queries.</param>
/// <param name="ServerToken">Pelican application API token for server listing.</param>
/// <param name="ServerUrl">Base URL of the Pelican Panel instance.</param>
/// <param name="BotToken">Discord bot authentication token.</param>
/// <param name="ChannelIds">Discord channel IDs where status messages are posted.</param>
/// <param name="NotificationChannelId">Discord channel ID for update notifications (optional, defaults to first status channel).</param>
/// <param name="ExternalServerIp">Public IP to display for internal servers.</param>
public record Secrets(
    string? ClientToken,
    string? ServerToken,
    string? ServerUrl,
    string? BotToken,
    ulong[]? ChannelIds,
    ulong? NotificationChannelId,
    string? ExternalServerIp
);
