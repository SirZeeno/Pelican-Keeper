using DSharpPlus.Entities;

namespace Pelican_Keeper;

using static TemplateClasses;
using static ConsoleExt;

public static class LiveMessageStorage
{
    internal static readonly LiveMessageJsonStorage Cache;

    /// <summary>
    /// Entry point and initializer for the class.
    /// </summary>
    static LiveMessageStorage()
    {
        Cache = new LiveMessageJsonStorage();
    }

    /// <summary>
    /// Saves the message ID to the cache.
    /// </summary>
    /// <param name="messageId">discord message ID</param>
    public static void Save(ulong messageId)
    {
        if (Get(messageId) != null) return;
        
        Cache.LiveStore?.Add(messageId);
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
            Cache.PaginatedLiveStore?.Add(messageId, currentPageIndex);
    }
    
    public static void Remove(ulong? messageId)
    {
        if (messageId != null && Cache.LiveStore != null && Cache.LiveStore.Remove((ulong)messageId))
            WriteLine($"Message {messageId} removed from History", CurrentStep.MessageHistory, OutputType.Debug);
        
        if (messageId != null && Cache.PaginatedLiveStore != null && Cache.PaginatedLiveStore.Remove((ulong)messageId))
            WriteLine($"Message {messageId} removed from History", CurrentStep.MessageHistory, OutputType.Debug);
    }

    /// <summary>
    /// Checks if a message exists in a channel.
    /// </summary>
    /// <param name="channels">list of target channels</param>
    /// <param name="messageId">discord message ID</param>
    /// <returns>bool whether the message exists</returns>
    private static async Task<bool> MessageExistsAsync(List<DiscordChannel> channels, ulong messageId)
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
                WriteLine($"Message {messageId} not found in #{channel.Name}", CurrentStep.MessageHistory, OutputType.Warning);
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                WriteLine($"No permission to read #{channel.Name}", CurrentStep.MessageHistory, OutputType.Warning);
            }
            catch (DSharpPlus.Exceptions.BadRequestException ex)
            {
                WriteLine($"Bad request on #{channel.Name}: {ex.Message}", CurrentStep.MessageHistory, OutputType.Warning);
            }
        }

        if (channels.Count == 1) return false; // I am searching only one channel, so I don't need to log.
        
        WriteLine($"Message {messageId} not found in any channel", CurrentStep.MessageHistory, OutputType.Debug);
        return false;
    }
    
    /// <summary>
    /// Gets the message ID from the cache if it exists.
    /// </summary>
    /// <param name="messageId">discord message ID</param>
    /// <returns>discord message ID</returns>
    public static ulong? Get(ulong? messageId)
    {
        if (Cache.LiveStore == null || Cache.LiveStore.Count == 0 || messageId == null) return null;
        return Cache.LiveStore?.FirstOrDefault(x => x == messageId);
    }
    
    /// <summary>
    /// Gets the last message ID in the cache if it exists in the channel.
    /// </summary>
    /// <param name="channel">Discord Channel to search in</param>
    /// <returns>discord message ID</returns>
    public static ulong? TryGetLast(DiscordChannel channel)
    {
        ulong? lastMessageId = Cache.LiveStore?.LastOrDefault(x => MessageExistsAsync([channel], x).Result);
        if (lastMessageId != 0) return lastMessageId;
        lastMessageId = TryGetPreviousMessage(channel).GetAwaiter().GetResult();
        if (lastMessageId != 0) return lastMessageId;

        return null;
    }
    
    /// <summary>
    /// Gets the page index of the paginated message if it exists.
    /// </summary>
    /// <param name="messageId">discord message ID</param>
    /// <returns>page index</returns>
    public static int? GetPaginated(ulong? messageId)
    {
        if (Cache.PaginatedLiveStore == null || Cache.PaginatedLiveStore.Count == 0 || messageId == null) return null;
        return Cache.PaginatedLiveStore?.First(x => x.Key == messageId).Value;
    }

    /// <summary>
    /// Gets the Key Value Pair of the paginated message if it exists in the channel.
    /// </summary>
    /// <param name="channel">Discord Channel to search in</param>
    /// <returns>discord message ID</returns>
    public static KeyValuePair<ulong, int>? TryGetLastPaginated(DiscordChannel channel)
    {
        KeyValuePair<ulong, int>? lastPaginated = Cache.PaginatedLiveStore?.LastOrDefault(x => MessageExistsAsync([channel], x.Key).Result);

        if (lastPaginated!.Value.Key != 0) return lastPaginated;
        ulong? lastMessageId = TryGetPreviousMessage(channel).GetAwaiter().GetResult();
        if (lastMessageId != 0)
            return new KeyValuePair<ulong, int>(TryGetPreviousMessage(channel).GetAwaiter().GetResult(), 0);

        return null;
    }
    
    /// <summary>
    /// Gets the previous message if any authored by the bot
    /// </summary>
    /// <param name="channel">Discord Channel to check</param>
    /// <returns>Message ID if found or 0 if nothing was found</returns>
    private static async Task<ulong> TryGetPreviousMessage(DiscordChannel channel)
    {
        IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync();

        foreach (var message in messages)
        {
            if (message.Author.Id == Program.BotId)
            {
                WriteLine($"Previous message: {message.Id}", CurrentStep.MessageHistory, OutputType.Debug);
                if (Program.Config.MessageFormat != MessageFormat.Paginated)
                {
                    Cache.LiveStore ??= new HashSet<ulong>();
                    Save(message.Id);
                    WriteLine($"Message {message.Id} added to History", CurrentStep.MessageHistory, OutputType.Debug);
                }
                else
                {
                    Cache.PaginatedLiveStore ??= new Dictionary<ulong, int>();
                    Save(message.Id, 0);
                }
                return message.Id;
            }
        }
        
        return 0;
    }
}