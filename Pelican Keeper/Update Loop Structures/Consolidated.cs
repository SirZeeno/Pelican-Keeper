using DSharpPlus;
using DSharpPlus.Entities;
using Pelican_Keeper.Helper_Classes;

namespace Pelican_Keeper.Update_Loop_Structures;

using static ConsoleExt;
using static TemplateClasses;
using static HelperClass; 
using static PelicanInterface;
using static DiscordHelpers;

public static class Consolidated
{
    internal static void ConsolidatedUpdateLoop(DiscordClient client, ulong[] channelIds)
    {
        Config config = Program.Config;
        
        Program.StartEmbedUpdaterLoop(
            MessageFormat.Consolidated,
            async () =>
            {
                var serversList = GetServersList();
                Program.GlobalServerInfo = serversList;
                if (serversList.Count == 0)
                {
                    WriteLine("No servers found on Pelican.", CurrentStep.None, OutputType.Error);
                }
                var uuids = serversList.Select(s => s.Uuid).ToList();
                var embed = await Program.EmbedService.BuildMultiServerEmbed(serversList);
                return (uuids, embed)!;
            },
            async (embedObj, uuids) =>
            {
                var embed = (DiscordEmbed)embedObj;
                if (EmbedHasChanged(uuids, embed))
                {
                    foreach (var channelId in channelIds)
                    {
                        var channel = await client.GetChannelAsync(channelId);
                        var lastMessage = LiveMessageStorage.TryGetLast(channel);

                        if (lastMessage is null or 0)
                        {
                            WriteLine($"Couldn't find existing message in {channel.Name}", CurrentStep.DiscordMessage, OutputType.Debug);
                        }
                        
                        List<DiscordComponent> buttons = ButtonCreation.ConsolidatedButtonCreation(uuids);
                        
                        if (lastMessage != null && lastMessage != 0 && !config.DryRun)
                        {
                            var msg = await channel.GetMessageAsync((ulong)lastMessage);
                            
                            WriteLine($"Updating message {lastMessage}", CurrentStep.DiscordMessage, OutputType.Debug);

                            if (EmbedLengthCheck(embed))
                            {
                                await msg.ModifyAsync(mb =>
                                {
                                    mb.WithEmbed(embed);
                                    mb.ClearComponents();
                                    mb.AddRows(buttons);
                                });
                            }
                            else
                            {
                                WriteLine("Discord Message Embed Size Check Failed. Message Not Sent!", CurrentStep.DiscordMessage, OutputType.Error);
                            }
                        }
                        
                        else if (!config.DryRun)
                        {
                            if (EmbedLengthCheck(embed))
                            {
                                var msg = await channel.SendMessageAsync(mb =>
                                {
                                    mb.WithEmbed(embed);
                                    mb.ClearComponents();
                                    mb.AddRows(buttons);
                                });
                                LiveMessageStorage.Save(msg.Id);
                            }
                            else
                            {
                                WriteLine("Discord Message Embed Size Check Failed. Message Not Sent!", CurrentStep.DiscordMessage, OutputType.Error);
                            }
                        }
                    }
                }
                else
                    WriteLine("Message has not changed. Skipping.", CurrentStep.DiscordMessage, OutputType.Debug);
            }, config.ServerUpdateInterval + Random.Shared.Next(0, config.ServerUpdateInterval / 2)
        );
    }
}