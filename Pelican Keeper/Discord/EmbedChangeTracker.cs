using DSharpPlus.Entities;

namespace Pelican_Keeper.Discord;

/// <summary>
/// Tracks embed hashes to detect changes and avoid unnecessary updates.
/// </summary>
public static class EmbedChangeTracker
{
    private static readonly Dictionary<string, string> LastEmbedHashes = new();

    /// <summary>
    /// Checks if an embed has changed from the last known state.
    /// </summary>
    /// <param name="uuids">Server UUIDs associated with the embed.</param>
    /// <param name="embed">The new embed to compare.</param>
    /// <returns>True if the embed content has changed.</returns>
    public static bool HasChanged(List<string> uuids, DiscordEmbed embed)
    {
        foreach (var uuid in uuids)
        {
            var hash = embed.Description + string.Join(",", embed.Fields.Select(f => f.Name + f.Value));

            if (LastEmbedHashes.TryGetValue(uuid, out var lastHash) && lastHash == hash)
                return false;

            LastEmbedHashes[uuid] = hash;
        }

        return true;
    }

    /// <summary>
    /// Clears the hash cache.
    /// </summary>
    public static void Clear() => LastEmbedHashes.Clear();
}
