using DSharpPlus;
using DSharpPlus.Entities;
using Pelican_Keeper.Helper_Classes;
using Pelican_Keeper.Logging;

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
        var config = Program.Config;

        Program.StartEmbedUpdaterLoop(
            MessageFormat.Consolidated,
            async () =>
            {
                var serversList = GetServersList();
                Program.GlobalFilteredServerInfo = serversList.Item2;
                Program.GlobalFullServerInfo = serversList.Item1;
                if (serversList.Item2.Count == 0)
                    WriteLine("No servers found on Pelican that conform to your filters set in your configuration.", CurrentStep.None, OutputType.Error);
                var filteredUuids = serversList.Item2.Select(s => s.Uuid).ToList();
                var unfilteredUuids = serversList.Item1.Select(s => s.Uuid).ToList();
                var embed = await Program.EmbedService.BuildMultiServerEmbed(serversList.Item2);
                return (filteredUuids, serversList.Item1, embed)!;
            },
            async (embedObj, uuids, fullServerList) =>
            {
                var embed = (DiscordEmbed)embedObj;
                if (EmbedHasChanged(uuids, embed))
                    foreach (var channelId in channelIds)
                    {
                        var channel = await client.GetChannelAsync(channelId);
                        var lastMessage = LiveMessageStorage.TryGetLast(channel);

                        if (lastMessage is null or 0)
                            WriteLine($"Couldn't find existing message in {channel.Name}", CurrentStep.DiscordMessage,
                                OutputType.Debug);

                        var buttons = ButtonCreation.ConsolidatedButtonCreation(fullServerList);

                        if (lastMessage != null && lastMessage != 0 && !config.DryRun)
                        {
                            var msg = await channel.GetMessageAsync((ulong)lastMessage);

                            WriteLine($"Updating message {lastMessage}", CurrentStep.DiscordMessage, OutputType.Debug);

                            if (EmbedLengthCheck(embed))
                                await msg.ModifyAsync(mb =>
                                {
                                    mb.WithEmbed(embed);
                                    mb.ClearComponents();
                                    mb.AddRows(buttons);
                                });
                            else
                                WriteLine("Discord Message Embed Size Check Failed. Message Not Sent!",
                                    CurrentStep.DiscordMessage, OutputType.Error);
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
                                WriteLine("Discord Message Embed Size Check Failed. Message Not Sent!",
                                    CurrentStep.DiscordMessage, OutputType.Error);
                            }
                        }
                    }
                else
                    WriteLine("Message has not changed. Skipping.", CurrentStep.DiscordMessage, OutputType.Debug);
            }, config.ServerUpdateInterval + Random.Shared.Next(0, config.ServerUpdateInterval / 2)
        );
    }
}