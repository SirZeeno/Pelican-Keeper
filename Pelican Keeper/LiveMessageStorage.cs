using System.Security.Principal;
using System.Text.Json;
using DSharpPlus.Entities;

namespace Pelican_Keeper;

using static TemplateClasses;
using static ConsoleExt;

public static class LiveMessageStorage
{
    private static string _historyFilePath = "MessageHistory.json";

    internal static readonly LiveMessageJsonStorage? Cache;

    /// <summary>
    /// Entry point and initializer for the class.
    /// </summary>
    static LiveMessageStorage()
    {
        Cache = LoadAll();
        
        if (Cache is { LiveStore: null })
            WriteLineWithStepPretext("Failed to read MessageHistory.json!", CurrentStep.MessageHistory, OutputType.Error, new FileLoadException(), true);

        if (Cache is { LiveStore: not null })
        {
            foreach (var liveStore in Cache.LiveStore)
                WriteLineWithStepPretext($"Cache contents: {liveStore}", CurrentStep.MessageHistory);
        }

        _ = ValidateCache();
    }

    /// <summary>
    /// Loads the cache from the file.
    /// </summary>
    public static LiveMessageJsonStorage? LoadAll(string? customDirectoryOrFile = null)
    {
        string historyFilePath = FileManager.GetCustomFilePath("MessageHistory.json", customDirectoryOrFile);


        if (historyFilePath == string.Empty)
        {
            WriteLineWithStepPretext("MessageHistory.json not found. Creating default one.", CurrentStep.MessageHistory, OutputType.Warning);

            if (string.IsNullOrEmpty(customDirectoryOrFile))
            {
                using var file = File.Create("MessageHistory.json");
                using var writer = new StreamWriter(file);
                writer.Write(JsonSerializer.Serialize(new LiveMessageJsonStorage()));
                historyFilePath = FileManager.GetFilePath("MessageHistory.json");
            }
            else
            {
                WriteLineWithStepPretext("Custom File or Directory specified, but unable to find MessageHistory File there!",  CurrentStep.FileReading, OutputType.Error,  new FileLoadException(), true);
                return null;
            }

            if (historyFilePath == string.Empty)
            {
                WriteLineWithStepPretext("Unable to Find MessageHistory.json!", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true);
                return null;
            }
        }

        try
        {
            var json = File.ReadAllText(historyFilePath);
            _historyFilePath = historyFilePath;
            WriteLineWithStepPretext($"Loaded MessageHistory.json from location: {historyFilePath}", CurrentStep.MessageHistory);
            return JsonSerializer.Deserialize<LiveMessageJsonStorage>(json) ?? new LiveMessageJsonStorage();
        }
        catch (Exception ex)
        {
            WriteLineWithStepPretext($"Error loading live message cache! It may be corrupt or not in the right format. Simple solution is to delete the MessageHistory.json file and letting the bot recreate it. Message History File Path: {historyFilePath}", CurrentStep.MessageHistory, OutputType.Error, ex);
            return new LiveMessageJsonStorage();
        }
    }

    /// <summary>
    /// Saves the message ID to the cache.
    /// </summary>
    /// <param name="messageId">discord message ID</param>
    public static void Save(ulong messageId)
    {
        if (Get(messageId) != null) return;
        
        Cache?.LiveStore?.Add(messageId);
        File.WriteAllText(_historyFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions { WriteIndented = true }));
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
            Cache?.PaginatedLiveStore?.Add(messageId, currentPageIndex);
        
        File.WriteAllText(_historyFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions { WriteIndented = true }));
    }
    
    public static void Remove(ulong? messageId)
    {
        if (Cache != null && messageId != null && Cache.LiveStore != null && Cache.LiveStore.Remove((ulong)messageId))
            File.WriteAllText(_historyFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions { WriteIndented = true }));
        
        if (Cache != null && messageId != null && Cache.PaginatedLiveStore != null && Cache.PaginatedLiveStore.Remove((ulong)messageId))
            File.WriteAllText(_historyFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions { WriteIndented = true }));
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
            var filtered = await Cache.LiveStore.ToAsyncEnumerable().WhereAwait(async id => haveChannels && await MessageExistsAsync(channels!, id)).ToHashSetAsync();
            Cache.LiveStore = filtered;
        }

        if (Cache is { PaginatedLiveStore: not null })
        {
            var filtered = await Cache.PaginatedLiveStore.ToAsyncEnumerable().WhereAwait(async kvp => haveChannels && await MessageExistsAsync(channels!, kvp.Key)).ToDictionaryAsync(kvp => kvp.Key, kvp => kvp.Value);
            Cache.PaginatedLiveStore = filtered;
        }

        await File.WriteAllTextAsync(_historyFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions { WriteIndented = true }));
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
}