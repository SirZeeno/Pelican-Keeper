using DSharpPlus;
using DSharpPlus.Entities;

namespace Pelican_Keeper.Update_Loop_Structures;

using static ConsoleExt;
using static TemplateClasses;
using static HelperClass; 
using static PelicanInterface;

public static class Paginated
{
    internal static void PaginatedUpdateLoop(DiscordClient client, ulong[] channelIds)
    {
        Config config = Program.Config;
        
        Program.StartEmbedUpdaterLoop(
            MessageFormat.Paginated,
            async () =>
            {
                var serversList = GetServersList();
                Program.GlobalServerInfo = serversList;
                if (serversList.Count == 0)
                {
                    WriteLine("No servers found on Pelican.", CurrentStep.None, OutputType.Error);
                }
                var uuids = serversList.Select(s => s.Uuid).ToList();
                var embeds = await Program.EmbedService.BuildPaginatedServerEmbeds(serversList);
                return (uuids, embeds)!;
            },
            async (embedObj, uuids) =>
            {
                var embeds = (List<DiscordEmbed>)embedObj;
                
                if (LiveMessageStorage.Cache is { PaginatedLiveStore: not null })
                {
                    foreach (var channelId in channelIds)
                    {
                        var channel = await client.GetChannelAsync(channelId);
                        await channel.SendPaginatedMessageAsync(embeds, uuids);
                    }
                }
            }, config.ServerUpdateInterval + Random.Shared.Next(0, config.ServerUpdateInterval / 2)
        );
    }

    /// <summary>
    /// Sends a paginated message
    /// </summary>
    /// <param name="channel">Target channel</param>
    /// <param name="embeds">List of embeds to paginate</param>
    /// <param name="uuids">List of UUIDs of servers</param>
    /// <returns>The discord message</returns>
    private static async Task SendPaginatedMessageAsync(this DiscordChannel channel, List<DiscordEmbed> embeds, List<string?> uuids)
    {
        Config config = Program.Config;
        var lastMessage = LiveMessageStorage.TryGetLastPaginated(channel);
        int index = 0;
        if (lastMessage != null)
        {
            index = lastMessage.Value.Value;
        }
        bool allEmbedsPassed = true;
                        
        if (lastMessage == null)
        {
            WriteLine($"Couldn't find existing message in {channel.Name}", CurrentStep.DiscordMessage, OutputType.Debug);
        }
        
        foreach (var embed in embeds)
        {
            if (EmbedLengthCheck(embed)) continue;
            allEmbedsPassed = false;
            WriteLine($"The Embed for {embed.Title} Failed Its Size Check", CurrentStep.DiscordMessage, OutputType.Error);
        }
        
        List<DiscordComponent> buttons = ButtonCreation.PaginatedButtonCreation(uuids, index);

        DiscordMessage? message = null;

        if (lastMessage != null && !config.DryRun && allEmbedsPassed)
        {
            Program.EmbedPages = embeds;
            
            // Keeps the current page index instead of resetting to 0
            var currentIndex = lastMessage.Value.Value;
            var updatedEmbed = embeds[currentIndex];
            
            var msg = await channel.GetMessageAsync(lastMessage.Value.Key);
            
            if (EmbedHasChanged(uuids, updatedEmbed))
            {
                WriteLine($"Updating paginated message {lastMessage.Value.Key} on page {currentIndex}", CurrentStep.DiscordMessage, OutputType.Debug);
                message = await msg.ModifyAsync(mb =>
                {
                    mb.WithEmbed(updatedEmbed);
                    mb.ClearComponents();
                    mb.AddComponents(buttons);
                });
            }
            else
            {
                WriteLine("Message has not changed. Skipping.", CurrentStep.DiscordMessage, OutputType.Debug);
            }
        }
        else if (allEmbedsPassed)
            message = await channel.SendMessageAsync(mb =>
            {
                mb.WithEmbed(embeds[0]);
                mb.ClearComponents();
                mb.AddComponents(buttons);
            });
        else
            WriteLine("Not every Embed passed its size check. Message Not Sent!", CurrentStep.DiscordMessage, OutputType.Error);

        if (message != null)
        {
            LiveMessageStorage.Save(message.Id, index);
        }
    }
}