using System.Text.Json;
using DSharpPlus.Entities;

namespace Pelican_Keeper;

using static TemplateClasses;
using static ConsoleExt;

public static class LiveMessageStorage
{
    private const string HistoryFilePath = "MessageHistory.json";

    internal static LiveMessageJsonStorage? Cache = new();

    /// <summary>
    /// Gets the page index of a paginated message if it exists.
    /// </summary>
    /// <param name="messageId">discord message ID</param>
    /// <returns>page index</returns>
    public static int? GetPaginated(ulong? messageId)
    {
        if (Cache?.PaginatedLiveStore == null || Cache.PaginatedLiveStore.Count == 0 || messageId == null) return null;
        return Cache.PaginatedLiveStore?.First(x => x.Key == messageId).Value;
    }

    /// <summary>
    /// Entry point and initializer for the class.
    /// </summary>
    static LiveMessageStorage()
    {
        LoadAll();
        _ = ValidateCache();
    }

    /// <summary>
    /// Loads the cache from the file.
    /// </summary>
    private static void LoadAll()
    {
        if (!File.Exists(HistoryFilePath))
        {
            WriteLineWithStepPretext("MessageHistory.json not found. Creating default one.", CurrentStep.MessageHistory, OutputType.Warning);
            using var file = File.Create("MessageHistory.json");
            using var writer = new StreamWriter(file);
            writer.Write(JsonSerializer.Serialize(new LiveMessageJsonStorage()));
        }

        try
        {
            var json = File.ReadAllText(HistoryFilePath);
            Cache = JsonSerializer.Deserialize<LiveMessageJsonStorage>(json) ?? new LiveMessageJsonStorage();
            foreach (var liveStore in Cache.LiveStore)
            {
                WriteLineWithStepPretext($"Cache contents: {liveStore}", CurrentStep.MessageHistory);
            }
        }
        catch (Exception ex)
        {
            WriteLineWithStepPretext("Error loading live message cache! It may be corrupt or not in the right format. Simple solution is to delete the MessageHistory.json file and letting the bot recreate it.", CurrentStep.MessageHistory, OutputType.Error, ex);
            Cache = new LiveMessageJsonStorage();
        }
    }

    /// <summary>
    /// Saves the message ID to the cache.
    /// </summary>
    /// <param name="messageId">discord message ID</param>
    public static void Save(ulong messageId)
    {
        if (Get(messageId) != null)
        {
            return;
        }
        
        Cache?.LiveStore?.Add(messageId);
        File.WriteAllText(HistoryFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
    
    /// <summary>
    /// Saves the page index of a paginated message.
    /// </summary>
    /// <param name="messageId">discord message ID</param>
    /// <param name="currentPageIndex">page index</param>
    public static void Save(ulong messageId, int currentPageIndex)
    {
        if (Cache is { PaginatedLiveStore: not null } && Cache.PaginatedLiveStore.ContainsKey(messageId))
        {
            if (Cache.PaginatedLiveStore[messageId] == currentPageIndex) return;
            Cache.PaginatedLiveStore[messageId] = currentPageIndex;
        }
        else
        {
            Cache?.PaginatedLiveStore?.Add(messageId, currentPageIndex);
        }
        File.WriteAllText(HistoryFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
    
    public static void Remove(ulong? messageId)
    {
        if (Cache != null && messageId != null && Cache.LiveStore != null && Cache.LiveStore.Remove((ulong)messageId))
        {
            File.WriteAllText(HistoryFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        if (Cache != null && messageId != null && Cache.PaginatedLiveStore != null && Cache.PaginatedLiveStore.Remove((ulong)messageId))
        {
            File.WriteAllText(HistoryFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
    }
    
    /// <summary>
    /// Validates the cache and removes any messages that no longer exist in the configured channels.
    /// </summary>
    private static async Task ValidateCache()
    {
        var channels = Program.TargetChannel;
        bool haveChannels = channels is { Count: > 0 };

        if (Cache is { LiveStore: not null })
        {
            var filtered = await Cache.LiveStore
                .ToAsyncEnumerable()
                .WhereAwait(async id =>
                    haveChannels && await MessageExistsAsync(channels!, id))
                .ToHashSetAsync();
            Cache.LiveStore = filtered;
        }

        if (Cache is { PaginatedLiveStore: not null })
        {
            var filtered = await Cache.PaginatedLiveStore
                .ToAsyncEnumerable()
                .WhereAwait(async kvp =>
                    haveChannels && await MessageExistsAsync(channels!, kvp.Key))
                .ToDictionaryAsync(kvp => kvp.Key, kvp => kvp.Value);
            Cache.PaginatedLiveStore = filtered;
        }

        await File.WriteAllTextAsync(
            HistoryFilePath,
            JsonSerializer.Serialize(Cache, new JsonSerializerOptions { WriteIndented = true })
        );
    }


    /// <summary>
    /// Checks if a message exists in a channel.
    /// </summary>
    /// <param name="channels">list of target channels</param>
    /// <param name="messageId">discord message ID</param>
    /// <returns>bool whether the message exists</returns>
    public static async Task<bool> MessageExistsAsync(List<DiscordChannel> channels, ulong messageId)
    {
        if (channels is not { Count: > 0 }) return true;

        foreach (var channel in channels)
        {
            try
            {
                var msg = await channel.GetMessageAsync(messageId);
                if (msg != null) return true;
            }
            catch (DSharpPlus.Exceptions.NotFoundException)
            {
                if (Program.Config.Debug)
                    WriteLineWithStepPretext($"Message {messageId} not found in #{channel.Name}", CurrentStep.MessageHistory, OutputType.Warning);
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                if (Program.Config.Debug)
                    WriteLineWithStepPretext($"No permission to read #{channel.Name}", CurrentStep.MessageHistory, OutputType.Warning);
            }
            catch (DSharpPlus.Exceptions.BadRequestException ex)
            {
                if (Program.Config.Debug)
                    WriteLineWithStepPretext($"Bad request on #{channel.Name}: {ex.Message}", CurrentStep.MessageHistory, OutputType.Warning);
            }
        }

        if (channels.Count == 1) return false; // I am searching only one channel, so I don't need to log.
        
        if (Program.Config.Debug)
            WriteLineWithStepPretext($"Message {messageId} not found in any channel", CurrentStep.MessageHistory, OutputType.Error);
        return false;
    }
    
    /// <summary>
    /// Gets the message ID from the cache if it exists.
    /// </summary>
    /// <param name="messageId">discord message ID</param>
    /// <returns>discord message ID</returns>
    public static ulong? Get(ulong? messageId)
    {
        if (Cache?.LiveStore == null || Cache.LiveStore.Count == 0 || messageId == null) return null;
        return Cache.LiveStore?.FirstOrDefault(x => x == messageId);
    }
}