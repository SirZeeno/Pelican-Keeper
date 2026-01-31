using DSharpPlus;
using DSharpPlus.Entities;

namespace Pelican_Keeper.Update_Loops;

using static ConsoleExt;
using static TemplateClasses;
using static HelperClass; 
using static PelicanInterface;

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
                    WriteLine("No servers found on Pelican.", CurrentStep.Ignore, OutputType.Error);
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
                        var tracked = LiveMessageStorage.TryGetLast(channel);
                        
                        if (tracked != null && tracked != 0 && !config.DryRun)
                        {
                            var msg = await channel.GetMessageAsync((ulong)tracked);
                            if (config.Debug)
                                WriteLine($"Updating message {tracked}");

                            if (config is { AllowUserServerStartup: true, IgnoreOfflineServers: false } or {AllowUserServerStopping: true})
                            {
                                List<string?> selectedServerUuids = uuids;

                                if (config.AllowServerStartup is { Length: > 0 } && !string.Equals(config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal))
                                {
                                    selectedServerUuids = selectedServerUuids.Where(uuid => config.AllowServerStartup.Contains(uuid)).ToList();
                                }
                                
                                if (config.AllowServerStopping is { Length: > 0 } && !string.Equals(config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal))
                                {
                                    selectedServerUuids = selectedServerUuids.Where(uuid => config.AllowServerStopping.Contains(uuid)).ToList();
                                }

                                List<DiscordComponent> buttons = [];
                                
                                // Build START menus
                                if (config.AllowUserServerStartup)
                                {
                                    var startOptions = selectedServerUuids.Select((uuid, i) =>
                                        new DiscordSelectComponentOption(
                                            label: Program.GlobalServerInfo[i].Name,     // shown to user
                                            value: uuid                     // data you read on interaction
                                        )
                                    );

                                    foreach (var group in Chunk(startOptions, 25))
                                    {
                                        var startMenu = new DiscordSelectComponent(
                                            customId: "start_menu",
                                            placeholder: "Start a server…",
                                            options: group,
                                            minOptions: 1,
                                            maxOptions: 1,
                                            disabled: false
                                        );
                                        buttons.Add(startMenu);
                                    }
                                }
                                
                                // Build STOP menus
                                if (config.AllowUserServerStopping)
                                {
                                    var stopOptions = selectedServerUuids.Select((uuid, i) =>
                                        new DiscordSelectComponentOption(
                                            label: Program.GlobalServerInfo[i].Name,
                                            value: uuid
                                        )
                                    );

                                    foreach (var group in Chunk(stopOptions, 25))
                                    {
                                        var stopMenu = new DiscordSelectComponent(
                                            customId: "stop_menu",
                                            placeholder: "Stop a server…",
                                            options: group,
                                            minOptions: 1,
                                            maxOptions: 1,
                                            disabled: false
                                        );
                                        buttons.Add(stopMenu);
                                    }
                                }

                                WriteLine("Buttons created: " + buttons.Count);
                                await msg.ModifyAsync(mb =>
                                {
                                    mb.WithEmbed(embed);
                                    mb.AddRows(buttons);
                                });
                            }
                            else
                            {
                                await msg.ModifyAsync(mb =>
                                {
                                    mb.WithEmbed(embed);
                                    mb.ClearComponents();
                                });
                            }
                        }
                        else if (config.DryRun)
                        {
                            if (config is { AllowUserServerStartup: true, IgnoreOfflineServers: false } or {AllowUserServerStopping: true})
                            {
                                List<string?> selectedServerUuids = uuids;
                                
                                if (config.AllowServerStartup is { Length: > 0 } && !string.Equals(config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal))
                                {
                                    selectedServerUuids = selectedServerUuids.Where(uuid => config.AllowServerStartup.Contains(uuid)).ToList();
                                    WriteLine($"Selected Servers: {selectedServerUuids.Count}", CurrentStep.Ignore, OutputType.Warning);
                                }
                                
                                if (config.AllowServerStopping is { Length: > 0 } && !string.Equals(config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal))
                                {
                                    selectedServerUuids = selectedServerUuids.Where(uuid => config.AllowServerStopping.Contains(uuid)).ToList();
                                }

                                List<DiscordComponent> buttons = [];
                                // Build START menus
                                if (config.AllowUserServerStartup)
                                {
                                    var startOptions = selectedServerUuids.Select((uuid, i) =>
                                        new DiscordSelectComponentOption(
                                            label: Program.GlobalServerInfo[i].Name,     // shown to user
                                            value: uuid                          // data it reads on interaction
                                        )
                                    );

                                    foreach (var group in Chunk(startOptions, 25))
                                    {
                                        var startMenu = new DiscordSelectComponent(
                                            customId: "start_menu",
                                            placeholder: "Start a server…",
                                            options: group,
                                            minOptions: 1,
                                            maxOptions: 1,
                                            disabled: false
                                        );
                                        buttons.Add(startMenu);
                                    }
                                }
                                
                                // Build STOP menus
                                if (config.AllowUserServerStopping)
                                {
                                    var stopOptions = selectedServerUuids.Select((uuid, i) =>
                                        new DiscordSelectComponentOption(
                                            label: Program.GlobalServerInfo[i].Name,
                                            value: uuid
                                        )
                                    );

                                    foreach (var group in Chunk(stopOptions, 25))
                                    {
                                        var stopMenu = new DiscordSelectComponent(
                                            customId: "stop_menu",
                                            placeholder: "Stop a server…",
                                            options: group,
                                            minOptions: 1,
                                            maxOptions: 1,
                                            disabled: false
                                        );
                                        buttons.Add(stopMenu);
                                    }
                                }
                                
                                WriteLine("Buttons created: " + buttons.Count);
                                DebugDumpComponents(buttons);
                                var msg = await channel.SendMessageAsync(mb =>
                                {
                                    mb.WithEmbed(embed);
                                    mb.AddRows(buttons);
                                });
                                LiveMessageStorage.Save(msg.Id);
                            }
                            else
                            {
                                var msg = await channel.SendMessageAsync(embed);
                                LiveMessageStorage.Save(msg.Id);
                            }
                        }
                    }
                }
                else if (config.Debug)
                    WriteLine("Message has not changed. Skipping.");
            }, config.ServerUpdateInterval + Random.Shared.Next(0, config.ServerUpdateInterval / 2)
        );
    }
}