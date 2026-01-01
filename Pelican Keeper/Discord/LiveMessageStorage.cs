using System.Text.Json;
using DSharpPlus.Entities;
using Pelican_Keeper.Core;
using Pelican_Keeper.Models;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Discord;

/// <summary>
/// Manages persistent tracking of Discord message IDs for editing instead of re-creating.
/// </summary>
public static class LiveMessageStorage
{
    private static string _historyFilePath = "MessageHistory.json";
    internal static LiveMessageJsonStorage? Cache { get; private set; }

    static LiveMessageStorage()
    {
        Cache = LoadAll();

        if (Cache?.LiveStore == null)
        {
            Logger.WriteLineWithStep("Failed to read MessageHistory.json. Initializing new in-memory cache.", Logger.Step.MessageHistory, Logger.OutputType.Warning);
            Cache = new LiveMessageJsonStorage();
        }

        foreach (var id in Cache.LiveStore!)
            Logger.WriteLineWithStep($"Cached message ID: {id}", Logger.Step.MessageHistory);

        _ = ValidateCacheAsync();
    }

    /// <summary>
    /// Loads the message history from disk.
    /// </summary>
    public static LiveMessageJsonStorage? LoadAll(string? customPath = null)
    {
        var path = Configuration.FileManager.GetCustomFilePath("MessageHistory.json", customPath);

        if (path == string.Empty)
        {
            Logger.WriteLineWithStep("MessageHistory.json not found. Creating default.", Logger.Step.MessageHistory, Logger.OutputType.Warning);

            if (!string.IsNullOrEmpty(customPath))
            {
                Logger.WriteLineWithStep("Custom path specified but file not found.", Logger.Step.FileReading, Logger.OutputType.Error, shouldExit: true);
                return null;
            }

            using var file = File.Create("MessageHistory.json");
            using var writer = new StreamWriter(file);
            writer.Write(JsonSerializer.Serialize(new LiveMessageJsonStorage()));
            path = Configuration.FileManager.GetFilePath("MessageHistory.json");

            if (path == string.Empty)
            {
                Logger.WriteLineWithStep("Unable to find MessageHistory.json after creation.", Logger.Step.FileReading, Logger.OutputType.Error, shouldExit: true);
                return null;
            }
        }

        try
        {
            var json = File.ReadAllText(path);
            _historyFilePath = path;
            Logger.WriteLineWithStep($"Loaded MessageHistory.json from: {path}", Logger.Step.MessageHistory);
            return JsonSerializer.Deserialize<LiveMessageJsonStorage>(json) ?? new LiveMessageJsonStorage();
        }
        catch (Exception ex)
        {
            Logger.WriteLineWithStep("Error loading message cache. Delete MessageHistory.json to recreate.", Logger.Step.MessageHistory, Logger.OutputType.Error, ex);
            return new LiveMessageJsonStorage();
        }
    }

    /// <summary>
    /// Saves a message ID to the cache (non-paginated).
    /// </summary>
    public static void Save(ulong messageId)
    {
        if (Get(messageId) != null) return;

        Cache?.LiveStore?.Add(messageId);
        PersistCache();
    }

    /// <summary>
    /// Saves a paginated message with its current page index.
    /// </summary>
    public static void Save(ulong messageId, int pageIndex)
    {
        if (Cache?.PaginatedLiveStore == null) return;

        if (Cache.PaginatedLiveStore.TryGetValue(messageId, out var existing))
        {
            if (existing == pageIndex) return;
            Cache.PaginatedLiveStore[messageId] = pageIndex;
        }
        else
        {
            Cache.PaginatedLiveStore[messageId] = pageIndex;
        }

        PersistCache();
    }

    /// <summary>
    /// Removes a message ID from the cache.
    /// </summary>
    public static void Remove(ulong? messageId)
    {
        if (messageId == null || Cache == null) return;

        var removed = Cache.LiveStore?.Remove((ulong)messageId) ?? false;
        removed |= Cache.PaginatedLiveStore?.Remove((ulong)messageId) ?? false;

        if (removed) PersistCache();
    }

    /// <summary>
    /// Gets a message ID if it exists in the non-paginated cache.
    /// </summary>
    public static ulong? Get(ulong? messageId)
    {
        if (Cache?.LiveStore == null || messageId == null) return null;
        return Cache.LiveStore.FirstOrDefault(x => x == messageId);
    }

    /// <summary>
    /// Gets the page index for a paginated message.
    /// </summary>
    public static int? GetPaginated(ulong? messageId)
    {
        if (Cache?.PaginatedLiveStore == null || messageId == null) return null;
        return Cache.PaginatedLiveStore.TryGetValue((ulong)messageId, out var index) ? index : null;
    }

    /// <summary>
    /// Checks if a message exists in any of the specified channels.
    /// </summary>
    public static async Task<bool> MessageExistsAsync(List<DiscordChannel> channels, ulong messageId)
    {
        if (channels.Count == 0) return true;

        foreach (var channel in channels)
        {
            try
            {
                var msg = await channel.GetMessageAsync(messageId);
                if (msg != null) return true;
            }
            catch (DSharpPlus.Exceptions.NotFoundException)
            {
                if (RuntimeContext.Config.Debug)
                    Logger.WriteLineWithStep($"Message {messageId} not found in #{channel.Name}", Logger.Step.MessageHistory, Logger.OutputType.Warning);
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                if (RuntimeContext.Config.Debug)
                    Logger.WriteLineWithStep($"No permission to read #{channel.Name}", Logger.Step.MessageHistory, Logger.OutputType.Warning);
            }
            catch (DSharpPlus.Exceptions.BadRequestException ex)
            {
                if (RuntimeContext.Config.Debug)
                    Logger.WriteLineWithStep($"Bad request on #{channel.Name}: {ex.Message}", Logger.Step.MessageHistory, Logger.OutputType.Warning);
            }
        }

        if (channels.Count > 1 && RuntimeContext.Config.Debug)
            Logger.WriteLineWithStep($"Message {messageId} not found in any channel.", Logger.Step.MessageHistory, Logger.OutputType.Error);

        return false;
    }

    private static async Task ValidateCacheAsync()
    {
        var channels = RuntimeContext.TargetChannels;
        if (channels.Count == 0) return;

        if (Cache?.LiveStore != null)
        {
            var filtered = await Cache.LiveStore
                .ToAsyncEnumerable()
                .WhereAwait(async id => await MessageExistsAsync(channels, id))
                .ToHashSetAsync();
            Cache.LiveStore = filtered;
        }

        if (Cache?.PaginatedLiveStore != null)
        {
            var filtered = await Cache.PaginatedLiveStore
                .ToAsyncEnumerable()
                .WhereAwait(async kvp => await MessageExistsAsync(channels, kvp.Key))
                .ToDictionaryAsync(kvp => kvp.Key, kvp => kvp.Value);
            Cache.PaginatedLiveStore = filtered;
        }

        PersistCache();
    }

    private static void PersistCache()
    {
        File.WriteAllText(_historyFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>
    /// Saves a message ID to the cache asynchronously (non-paginated).
    /// </summary>
    public static Task SaveAsync(ulong messageId)
    {
        Save(messageId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves a paginated message with its current page index asynchronously.
    /// </summary>
    public static Task SaveAsync(ulong messageId, int pageIndex)
    {
        Save(messageId, pageIndex);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets an existing message ID from the cache for a specific channel.
    /// </summary>
    public static async Task<ulong?> GetExistingMessageIdAsync(DiscordChannel channel)
    {
        if (Cache?.LiveStore == null) return null;

        foreach (var id in Cache.LiveStore.ToList())
        {
            if (await MessageExistsAsync([channel], id))
                return id;
        }

        return null;
    }

    /// <summary>
    /// Gets an existing paginated message with its page index for a specific channel.
    /// </summary>
    public static async Task<(ulong? messageId, int? pageIndex)> GetExistingPaginatedMessageAsync(DiscordChannel channel)
    {
        if (Cache?.PaginatedLiveStore == null) return (null, null);

        foreach (var kvp in Cache.PaginatedLiveStore.ToList())
        {
            if (await MessageExistsAsync([channel], kvp.Key))
                return (kvp.Key, kvp.Value);
        }

        return (null, null);
    }
}
