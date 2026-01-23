using DSharpPlus;
using DSharpPlus.Entities;


namespace Pelican_Keeper.Update_Loops;

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
                    WriteLineWithPretext("No servers found on Pelican.", OutputType.Error);
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
                        var cacheEntry = LiveMessageStorage.TryGetLastPaginated(channel);
                        
                        if (cacheEntry != null && !config.DryRun)
                        {
                            Program.EmbedPages = embeds;

                            // Keeps the current page index instead of resetting to 0
                            var currentIndex = cacheEntry.Value.Value;
                            var updatedEmbed = embeds[currentIndex];
                                
                            var msg = await channel.GetMessageAsync(cacheEntry.Value.Key);

                            if (EmbedHasChanged(uuids, updatedEmbed))
                            {
                                if (config.Debug)
                                    WriteLineWithPretext($"Updating paginated message {cacheEntry.Value.Key} on page {currentIndex}");
                                await msg.ModifyAsync(updatedEmbed);
                            }
                            else if (config.Debug)
                                WriteLineWithPretext("Message has not changed. Skipping.");
                        }
                        else
                        {
                            if (!config.DryRun)
                            {
                                bool allowAllStart = config.AllowServerStartup == null || config.AllowServerStartup.Length == 0 || string.Equals(config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal);
                                bool showStart = config is { AllowUserServerStartup: true, IgnoreOfflineServers: false, AllowServerStartup: not null } && (allowAllStart || config.AllowServerStartup.Contains(uuids[0], StringComparer.OrdinalIgnoreCase));
                                    
                                WriteLineWithPretext("show all Start: " + allowAllStart);
                                WriteLineWithPretext("show Start: " + showStart);
                                    
                                bool allowAllStop = config.AllowServerStopping == null || config.AllowServerStopping.Length == 0 || string.Equals(config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal);
                                bool showStop = config is { AllowUserServerStopping: true, IgnoreOfflineServers: false, AllowServerStopping: not null } && (allowAllStop || config.AllowServerStopping.Contains(uuids[0], StringComparer.OrdinalIgnoreCase));
                                    
                                WriteLineWithPretext("show all Stop: " + allowAllStop);
                                WriteLineWithPretext("show Stop: " + showStop);
                                    
                                switch (showStart)
                                {
                                    case true when !showStop:
                                    {
                                        string? uuid = uuids[0];
                                        var msg = await channel.SendPaginatedMessageAsync(embeds, uuid);
                                        LiveMessageStorage.Save(msg.Id, 0);
                                        break;
                                    }
                                    case false when showStop:
                                    {
                                        string? uuid = uuids[0];
                                        var msg = await channel.SendPaginatedMessageAsync(embeds, null, uuid);
                                        LiveMessageStorage.Save(msg.Id, 0);
                                        break;
                                    }
                                }

                                if (showStart && showStop)
                                {
                                    string? uuid = uuids[0];
                                    var msg = await channel.SendPaginatedMessageAsync(embeds, uuid, uuid);
                                    LiveMessageStorage.Save(msg.Id, 0);
                                }
                                else
                                {
                                    var msg = await channel.SendPaginatedMessageAsync(embeds);
                                    LiveMessageStorage.Save(msg.Id, 0);
                                }
                            }
                        }
                    }
                }
            }, config.ServerUpdateInterval + Random.Shared.Next(0, config.ServerUpdateInterval / 2)
        );
    }
}