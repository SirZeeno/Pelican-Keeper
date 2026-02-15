using DSharpPlus;
using DSharpPlus.Entities;
using Pelican_Keeper.Helper_Classes;

namespace Pelican_Keeper.Update_Loop_Structures;

using static ConsoleExt;
using static TemplateClasses;
using static HelperClass; 
using static PelicanInterface;
using static DiscordHelpers;

public static class PerServer
{
    internal static void PerServerUpdateLoop(DiscordClient client, ulong[] channelIds)
    {
        Config config = Program.Config;
        
        var serversList = GetServersList();
        Program.GlobalServerInfo = serversList;
        if (serversList.Count == 0)
        {
            WriteLine("No servers found on Pelican.", CurrentStep.None, OutputType.Error);
            return;
        }
        foreach (var server in serversList)
        {
            Program.StartEmbedUpdaterLoop(
                MessageFormat.PerServer,
                async () =>
                {
                    var uuid = server.Uuid;

                    var embed = await Program.EmbedService.BuildSingleServerEmbed(server);
                    return ([uuid], embed);
                },
                async (embedObj, uuid) =>
                {
                    if (embedObj is not DiscordEmbed embed) return;
                    if (EmbedHasChanged(uuid, embed))
                    {
                        foreach (var channelId in channelIds)
                        {
                            var channel = await client.GetChannelAsync(channelId);
                            var lastMessage = LiveMessageStorage.TryGetLast(channel);
                            
                            if (lastMessage is null or 0)
                            {
                                WriteLine($"Couldn't find existing message in {channel.Name}", CurrentStep.DiscordMessage, OutputType.Debug);
                            }
                            
                            List<DiscordComponent> buttons = ButtonCreation.PerServerButtonCreation(uuid[0]); //uuid[0] there is only the current embed in the list

                            if (lastMessage != null && lastMessage != 0 && !config.DryRun)
                            {
                                var msg = await channel.GetMessageAsync((ulong)lastMessage);
                                WriteLine($"Updating message {lastMessage}", CurrentStep.DiscordMessage, OutputType.Debug);
                                
                                await msg.ModifyAsync(mb =>
                                {
                                    mb.WithEmbed(embed);
                                    mb.ClearComponents();
                                    mb.AddComponents(buttons);
                                });
                            }
                            else if (!config.DryRun)
                            {
                                if (EmbedLengthCheck(embed))
                                {
                                    var msg = await channel.SendMessageAsync(mb =>
                                    {
                                        mb.WithEmbed(embed);
                                        mb.ClearComponents();
                                        mb.AddComponents(buttons);
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
                    {
                        WriteLine("Message has not changed. Skipping.", CurrentStep.DiscordMessage, OutputType.Debug);
                    }
                }, config.ServerUpdateInterval + Random.Shared.Next(0, 3) // randomized per-server delay
            );
        }
    }
}