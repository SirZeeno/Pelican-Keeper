﻿using System.Text.Json;
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
            WriteLineWithPretext("MessageHistory.json not found. Creating default one.", OutputType.Warning);
            using var file = File.Create("MessageHistory.json");
            using var writer = new StreamWriter(file);
            writer.Write(JsonSerializer.Serialize(new LiveMessageJsonStorage()));
        }

        try
        {
            var json = File.ReadAllText(HistoryFilePath);
            Cache = JsonSerializer.Deserialize<LiveMessageJsonStorage>(json) ?? new LiveMessageJsonStorage();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading live message cache: {ex.Message}");
            Cache = new LiveMessageJsonStorage();
        }
    }

    /// <summary>
    /// Saves the message ID to the cache.
    /// </summary>
    /// <param name="messageId">discord message ID</param>
    public static void Save(ulong messageId)
    {
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
    /// Validates the cache and removes any messages that no longer exist in the channel.
    /// </summary>
    private static async Task ValidateCache()
    {
        if (Cache is { LiveStore: not null })
        {
            var filtered = await Cache.LiveStore
                .ToAsyncEnumerable()
                .WhereAwait(async x => Program.TargetChannel != null && await MessageExistsAsync(Program.TargetChannel, x))
                .ToHashSetAsync();
            Cache.LiveStore = filtered;
        }
        
        if (Cache is { PaginatedLiveStore: not null })
        {
            var filtered = await Cache.PaginatedLiveStore
                .ToAsyncEnumerable()
                .WhereAwait(async x => Program.TargetChannel != null && await MessageExistsAsync(Program.TargetChannel, x.Key)).ToDictionaryAsync(x => x.Key, x => x.Value);
            Cache.PaginatedLiveStore = filtered;
        }

        await File.WriteAllTextAsync(HistoryFilePath, JsonSerializer.Serialize(Cache, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    /// <summary>
    /// Checks if a message exists in a channel.
    /// </summary>
    /// <param name="channel">target channel</param>
    /// <param name="messageId">discord message ID</param>
    /// <returns>bool whether the message exists</returns>
    private static async Task<bool> MessageExistsAsync(DiscordChannel channel, ulong messageId)
    {
        try
        {
            var msg = await channel.GetMessageAsync(messageId);
            return msg != null;
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            WriteLineWithPretext("Message not found", OutputType.Warning);
            return false;
        }
    }
    
    /// <summary>
    /// Gets the message ID from the cache if it exists.
    /// </summary>
    /// <param name="messageId">discord message ID</param>
    /// <returns>discord message ID</returns>
    public static ulong? Get(ulong? messageId)
    {
        if (Cache?.LiveStore == null || Cache.LiveStore.Count == 0 || messageId == null) return null;
        return Cache.LiveStore?.First(x => x == messageId);
    }
}