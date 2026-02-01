using DSharpPlus;
using DSharpPlus.Entities;

namespace Pelican_Keeper.Update_Loops;

using static ConsoleExt;
using static TemplateClasses;
using static HelperClass; 
using static PelicanInterface;

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
                            var tracked = LiveMessageStorage.TryGetLast(channel);

                            if (tracked != null && tracked != 0 && !config.DryRun)
                            {
                                var msg = await channel.GetMessageAsync((ulong)tracked);
                                if (config.Debug)
                                    WriteLine($"Updating message {tracked}");
                                    
                                bool allowAll = config.AllowServerStartup == null || config.AllowServerStartup.Length == 0 || string.Equals(config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal);
                                bool showStart = config is { AllowUserServerStartup: true, IgnoreOfflineServers: false, AllowServerStartup: not null } && (allowAll || config.AllowServerStartup.Contains(uuid[0], StringComparer.OrdinalIgnoreCase));
                                    
                                bool allowAllStop = config.AllowServerStopping == null || config.AllowServerStopping.Length == 0 || string.Equals(config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal);
                                bool showStop = config is { AllowUserServerStopping: true, IgnoreOfflineServers: false, AllowServerStopping: not null } && (allowAllStop || config.AllowServerStopping.Contains(uuid[0], StringComparer.OrdinalIgnoreCase));

                                await msg.ModifyAsync(mb =>
                                {
                                    mb.WithEmbed(embed);
                                    mb.ClearComponents();
                                    if (showStart)
                                        mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"Start: {uuid[0]}", "Start"));
                                    if (showStop)
                                        mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"Stop: {uuid[0]}", "Stop"));
                                });
                            }
                            else
                            {
                                if (config.DryRun) continue;
                                bool allowAll = config.AllowServerStartup == null || config.AllowServerStartup.Length == 0 || string.Equals(config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal);
                                bool showStart = config is { AllowUserServerStartup: true, IgnoreOfflineServers: false, AllowServerStartup: not null } && (allowAll || config.AllowServerStartup.Contains(uuid[0], StringComparer.OrdinalIgnoreCase));
                                    
                                bool allowAllStop = config.AllowServerStopping == null || config.AllowServerStopping.Length == 0 || string.Equals(config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal);
                                bool showStop = config is { AllowUserServerStopping: true, IgnoreOfflineServers: false, AllowServerStopping: not null } && (allowAllStop || config.AllowServerStopping.Contains(uuid[0], StringComparer.OrdinalIgnoreCase));
                                    
                                var msg = await channel.SendMessageAsync(mb =>
                                {
                                    mb.WithEmbed(embed);
                                    if (showStart)
                                        mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"Start: {uuid[0]}", "Start"));
                                    if (showStop)
                                        mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"Stop: {uuid[0]}", "Stop"));
                                });
                                LiveMessageStorage.Save(msg.Id);
                            }
                        }
                    }
                    else if (config.Debug)
                    {
                        WriteLine("Message has not changed. Skipping.");
                    }
                }, config.ServerUpdateInterval + Random.Shared.Next(0, 3) // randomized per-server delay
            );
        }
    }
}