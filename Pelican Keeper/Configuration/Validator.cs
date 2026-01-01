using Pelican_Keeper.Models;

namespace Pelican_Keeper.Configuration;

/// <summary>
/// Validates configuration objects for required fields and valid values.
/// </summary>
public static class Validator
{
    /// <summary>
    /// Validates Secrets for required API credentials.
    /// </summary>
    /// <exception cref="ArgumentNullException">When secrets object is null.</exception>
    /// <exception cref="ArgumentException">When required fields are missing.</exception>
    public static void ValidateSecrets(Secrets? secrets)
    {
        if (secrets == null)
            throw new ArgumentNullException(nameof(secrets), "Secrets object is null.");

        if (string.IsNullOrWhiteSpace(secrets.ClientToken))
            throw new ArgumentException("ClientToken is required. Provide a valid Pelican client API token.");

        if (string.IsNullOrWhiteSpace(secrets.ServerToken))
            throw new ArgumentException("ServerToken is required. Provide a valid Pelican application API token.");

        if (string.IsNullOrWhiteSpace(secrets.ServerUrl))
            throw new ArgumentException("ServerUrl is required. Provide your Pelican Panel URL.");

        if (string.IsNullOrWhiteSpace(secrets.BotToken))
            throw new ArgumentException("BotToken is required. Provide a valid Discord bot token.");

        if (secrets.ChannelIds == null || secrets.ChannelIds.Length == 0)
            throw new ArgumentException("ChannelIds is required. Provide at least one Discord channel ID.");
    }

    /// <summary>
    /// Validates Config for required settings and valid ranges.
    /// </summary>
    /// <exception cref="ArgumentNullException">When config object is null.</exception>
    /// <exception cref="ArgumentException">When settings are invalid.</exception>
    public static void ValidateConfig(Config? config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config), "Config object is null.");

        if (string.IsNullOrEmpty(config.InternalIpStructure))
            throw new ArgumentException("InternalIpStructure is required. Example: 192.168.*.*");

        if (config.MessageFormat == MessageFormat.None)
            throw new ArgumentException("MessageFormat must be set to PerServer, Consolidated, or Paginated.");

        if (config.MessageSorting == MessageSorting.None)
            throw new ArgumentException("MessageSorting must be set to Name, Status, or Uptime.");

        if (config.MessageSortingDirection == MessageSortingDirection.None)
            throw new ArgumentException("MessageSortingDirection must be set to Ascending or Descending.");

        if (string.IsNullOrEmpty(config.EmptyServerTimeout))
            throw new ArgumentException("EmptyServerTimeout is required. Format: d:hh:mm");

        if (config.MarkdownUpdateInterval < 10)
            throw new ArgumentException("MarkdownUpdateInterval must be at least 10 seconds.");

        if (config.ServerUpdateInterval < 10)
            throw new ArgumentException("ServerUpdateInterval must be at least 10 seconds.");
    }
}
