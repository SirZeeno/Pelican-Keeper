using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;

namespace Pelican_Keeper;

using static TemplateClasses;
using static HelperClass; 
using static PelicanInterface;
using static ConsoleExt;
using static DiscordInteractions;


internal static class Program
{
    internal static List<DiscordChannel?> TargetChannel = null!;
    internal static Secrets Secrets = null!;
    internal static Config Config = null!;
    private static readonly EmbedBuilderService EmbedService = new();
    internal static List<DiscordEmbed> EmbedPages = null!;
    internal static List<ServerInfo> GlobalServerInfo = null!;

    private enum EmbedUpdateMode
    {
        Consolidated,
        Paginated,
        PerServer
    }


    private static async Task Main()
    {
        TargetChannel = [];
        
        await FileManager.ReadSecretsFile();
        await FileManager.ReadConfigFile();

        if (FileManager.GetFilePath("MessageMarkdown.txt") == string.Empty)
        {
            Console.WriteLine("MessageMarkdown.txt not found. Pulling Default from Github!");
            _ = FileManager.CreateMessageMarkdownFile();
        }

        GetGamesToMonitorFileAsync();
        ServerMarkdown.GetMarkdownFileContentAsync();

        if (Config.AutoUpdate)
        {
            await VersionUpdater.UpdateProgram();
        }

        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = Secrets.BotToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
        });

        discord.Ready += OnClientReady;
        discord.MessageDeleted += OnMessageDeleted;
        discord.ComponentInteractionCreated += OnPageFlipInteraction;
        discord.ComponentInteractionCreated += OnServerStartInteraction;
        discord.ComponentInteractionCreated += OnServerStopInteraction;
        discord.ComponentInteractionCreated += OnDropDownInteration;
        
        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
    
    /// <summary>
    /// Function that is called when the bot is ready to send messages.
    /// </summary>
    /// <param name="sender">DiscordClient</param>
    /// <param name="e">ReadyEventArgs</param>
    private static async Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        WriteLineWithPretext("Bot is connected and ready!");
        if (Secrets.ChannelIds != null)
        {
            foreach (var targetChannel in Secrets.ChannelIds)
            {
                var discordChannel = await sender.GetChannelAsync(targetChannel);
                TargetChannel.Add(discordChannel);
                WriteLineWithPretext($"Target channel: {discordChannel.Name}");
            }

            _ = StartStatsUpdater(sender, Secrets.ChannelIds);
        }
        else
        {
            WriteLineWithPretext("ChannelIds in the Secrets File is empty or not spelled correctly!", OutputType.Error);
        }
    }

    /// <summary>
    /// Starts the Sever statistics updater loop.
    /// </summary>
    /// <param name="client">Bot client</param>
    /// <param name="channelIds">Target channels</param>
    private static Task StartStatsUpdater(DiscordClient client, ulong[] channelIds)
    {
        // When the config is set to consolidate embeds
        if (Config.MessageFormat == MessageFormat.Consolidated)
        {
            StartEmbedUpdaterLoop(
                EmbedUpdateMode.Consolidated,
                async () =>
                {
                    var serversList = GetServersList();
                    GlobalServerInfo = serversList;
                    if (serversList.Count == 0)
                    {
                        WriteLineWithPretext("No servers found on Pelican.", OutputType.Error);
                    }
                    var uuids = serversList.Select(s => s.Uuid).ToList();
                    var embed = await EmbedService.BuildMultiServerEmbed(serversList);
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
                            var tracked = LiveMessageStorage.Get(LiveMessageStorage.Cache?.LiveStore?.LastOrDefault(x => LiveMessageStorage.MessageExistsAsync([channel], x).Result));
                        
                            if (tracked != null && tracked != 0 && !Config.DryRun)
                            {
                                var msg = await channel.GetMessageAsync((ulong)tracked);
                                if (Config.Debug)
                                    WriteLineWithPretext($"Updating message {tracked}");

                                if (Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false } or {AllowUserServerStopping: true})
                                {
                                    List<string?> selectedServerUuids = uuids;

                                    if (Config.AllowServerStartup is { Length: > 0 } && !string.Equals(Config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal))
                                    {
                                        selectedServerUuids = selectedServerUuids.Where(uuid => Config.AllowServerStartup.Contains(uuid)).ToList();
                                    }
                                
                                    if (Config.AllowServerStopping is { Length: > 0 } && !string.Equals(Config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal))
                                    {
                                        selectedServerUuids = selectedServerUuids.Where(uuid => Config.AllowServerStopping.Contains(uuid)).ToList();
                                    }

                                    List<DiscordComponent> buttons = [];
                                
                                    // Build START menus
                                    if (Config.AllowUserServerStartup)
                                    {
                                        var startOptions = selectedServerUuids.Select((uuid, i) =>
                                            new DiscordSelectComponentOption(
                                                label: GlobalServerInfo[i].Name,     // shown to user
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
                                    if (Config.AllowUserServerStopping)
                                    {
                                        var stopOptions = selectedServerUuids.Select((uuid, i) =>
                                            new DiscordSelectComponentOption(
                                                label: GlobalServerInfo[i].Name,
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

                                    WriteLineWithPretext("Buttons created: " + buttons.Count);
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
                            else
                            {
                                if (Config.DryRun) continue;
                                if (Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false } or {AllowUserServerStopping: true})
                                {
                                    List<string?> selectedServerUuids = uuids;
                                
                                    if (Config.AllowServerStartup is { Length: > 0 } && !string.Equals(Config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal))
                                    {
                                        selectedServerUuids = selectedServerUuids.Where(uuid => Config.AllowServerStartup.Contains(uuid)).ToList();
                                        WriteLineWithPretext($"Selected Servers: {selectedServerUuids.Count}", OutputType.Warning);
                                    }
                                
                                    if (Config.AllowServerStopping is { Length: > 0 } && !string.Equals(Config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal))
                                    {
                                        selectedServerUuids = selectedServerUuids.Where(uuid => Config.AllowServerStopping.Contains(uuid)).ToList();
                                    }

                                    List<DiscordComponent> buttons = [];
                                    // Build START menus
                                    if (Config.AllowUserServerStartup)
                                    {
                                        var startOptions = selectedServerUuids.Select((uuid, i) =>
                                            new DiscordSelectComponentOption(
                                                label: GlobalServerInfo[i].Name,     // shown to user
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
                                    if (Config.AllowUserServerStopping)
                                    {
                                        var stopOptions = selectedServerUuids.Select((uuid, i) =>
                                            new DiscordSelectComponentOption(
                                                label: GlobalServerInfo[i].Name,
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
                                
                                    WriteLineWithPretext("Buttons created: " + buttons.Count);
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
                    else if (Config.Debug)
                        WriteLineWithPretext("Message has not changed. Skipping.");
                },
                delaySeconds: Config.ServerUpdateInterval + Random.Shared.Next(0, Config.ServerUpdateInterval / 2)
            );
        }
        
        // When the config is set to paginate
        if (Config.MessageFormat == MessageFormat.Paginated)
        {
            StartEmbedUpdaterLoop(
                EmbedUpdateMode.Paginated,
                async () =>
                {
                    var serversList = GetServersList();
                    GlobalServerInfo = serversList;
                    if (serversList.Count == 0)
                    {
                        WriteLineWithPretext("No servers found on Pelican.", OutputType.Error);
                    }
                    var uuids = serversList.Select(s => s.Uuid).ToList();
                    var embeds = await EmbedService.BuildPaginatedServerEmbeds(serversList);
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
                            var cacheEntry = LiveMessageStorage.Cache.PaginatedLiveStore.LastOrDefault(x => LiveMessageStorage.MessageExistsAsync([channel], x.Key).Result);

                            var pagedTracked = LiveMessageStorage.GetPaginated(cacheEntry.Key);
                            if (pagedTracked != null && !Config.DryRun)
                            {
                                EmbedPages = embeds;

                                // Keeps the current page index instead of resetting to 0
                                var currentIndex = pagedTracked.Value;
                                var updatedEmbed = embeds[currentIndex];
                                
                                var msg = await channel.GetMessageAsync(cacheEntry.Key);

                                if (EmbedHasChanged(uuids, updatedEmbed))
                                {
                                    if (Config.Debug)
                                        WriteLineWithPretext($"Updating paginated message {cacheEntry.Key} on page {currentIndex}");
                                    await msg.ModifyAsync(updatedEmbed);
                                }
                                else if (Config.Debug)
                                    WriteLineWithPretext("Message has not changed. Skipping.");
                            }
                            else
                            {
                                if (!Config.DryRun)
                                {
                                    bool allowAllStart = Config.AllowServerStartup == null || Config.AllowServerStartup.Length == 0 || string.Equals(Config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal);
                                    bool showStart = Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false, AllowServerStartup: not null } && (allowAllStart || Config.AllowServerStartup.Contains(uuids[0], StringComparer.OrdinalIgnoreCase));
                                    
                                    WriteLineWithPretext("show all Start: " + allowAllStart);
                                    WriteLineWithPretext("show Start: " + showStart);
                                    
                                    bool allowAllStop = Config.AllowServerStopping == null || Config.AllowServerStopping.Length == 0 || string.Equals(Config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal);
                                    bool showStop = Config is { AllowUserServerStopping: true, IgnoreOfflineServers: false, AllowServerStopping: not null } && (allowAllStop || Config.AllowServerStopping.Contains(uuids[0], StringComparer.OrdinalIgnoreCase));
                                    
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
                },
                delaySeconds: Config.ServerUpdateInterval + Random.Shared.Next(0, Config.ServerUpdateInterval / 2)
            );
        }
        
        // When the config is set to PerServerMessages
        if (Config.MessageFormat == MessageFormat.PerServer)
        {
            var serversList = GetServersList();
            GlobalServerInfo = serversList;
            if (serversList.Count == 0)
            {
                WriteLineWithPretext("No servers found on Pelican.", OutputType.Error);
                return Task.CompletedTask;
            }
            foreach (var server in serversList)
            {
                StartEmbedUpdaterLoop(
                    EmbedUpdateMode.PerServer,
                    async () =>
                    {
                        var uuid = server.Uuid;

                        var embed = await EmbedService.BuildSingleServerEmbed(server);
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
                                var tracked = LiveMessageStorage.Get(LiveMessageStorage.Cache?.LiveStore?.LastOrDefault(x => LiveMessageStorage.MessageExistsAsync([channel], x).Result));

                                if (tracked != null && tracked != 0 && !Config.DryRun)
                                {
                                    var msg = await channel.GetMessageAsync((ulong)tracked);
                                    if (Config.Debug)
                                        WriteLineWithPretext($"Updating message {tracked}");
                                    
                                    bool allowAll = Config.AllowServerStartup == null || Config.AllowServerStartup.Length == 0 || string.Equals(Config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal);
                                    bool showStart = Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false, AllowServerStartup: not null } && (allowAll || Config.AllowServerStartup.Contains(uuid[0], StringComparer.OrdinalIgnoreCase));
                                    
                                    bool allowAllStop = Config.AllowServerStopping == null || Config.AllowServerStopping.Length == 0 || string.Equals(Config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal);
                                    bool showStop = Config is { AllowUserServerStopping: true, IgnoreOfflineServers: false, AllowServerStopping: not null } && (allowAllStop || Config.AllowServerStopping.Contains(uuid[0], StringComparer.OrdinalIgnoreCase));

                                    await msg.ModifyAsync(mb =>
                                    {
                                        mb.WithEmbed(embed);
                                        mb.ClearComponents();
                                        if (showStart)
                                            mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, uuid[0]!, "Start"));
                                        if (showStop)
                                            mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, uuid[0]!, "Stop"));
                                    });
                                }
                                else
                                {
                                    if (Config.DryRun) continue;
                                    bool allowAll = Config.AllowServerStartup == null || Config.AllowServerStartup.Length == 0 || string.Equals(Config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal);
                                    bool showStart = Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false, AllowServerStartup: not null } && (allowAll || Config.AllowServerStartup.Contains(uuid[0], StringComparer.OrdinalIgnoreCase));
                                    
                                    bool allowAllStop = Config.AllowServerStopping == null || Config.AllowServerStopping.Length == 0 || string.Equals(Config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal);
                                    bool showStop = Config is { AllowUserServerStopping: true, IgnoreOfflineServers: false, AllowServerStopping: not null } && (allowAllStop || Config.AllowServerStopping.Contains(uuid[0], StringComparer.OrdinalIgnoreCase));
                                    
                                    var msg = await channel.SendMessageAsync(mb =>
                                    {
                                        mb.WithEmbed(embed);
                                        if (showStart)
                                            mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, uuid[0]!, "Start"));
                                        if (showStop)
                                            mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, uuid[0]!, "Stop"));
                                    });
                                    LiveMessageStorage.Save(msg.Id);
                                }
                            }
                        }
                        else if (Config.Debug)
                        {
                            WriteLineWithPretext("Message has not changed. Skipping.");
                        }
                    },
                    delaySeconds: Config.ServerUpdateInterval + Random.Shared.Next(0, 3) // randomized per-server delay
                );
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the embed updater loop called by the StartstatsUpdater method
    /// </summary>
    /// <param name="mode">The EmbedUpdateMode to use</param>
    /// <param name="generateEmbedsAsync">A function that generates the embeds</param>
    /// <param name="applyEmbedUpdateAsync">A function that applies the embed update</param>
    /// <param name="delaySeconds">Delay in seconds between updates</param>
    private static void StartEmbedUpdaterLoop(EmbedUpdateMode mode, Func<Task<(List<string?> uuids, object embedOrEmbeds)>> generateEmbedsAsync, Func<object, List<string?>, Task> applyEmbedUpdateAsync, int delaySeconds = 10)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    var (uuids, embedData) = await generateEmbedsAsync();
                    await applyEmbedUpdateAsync(embedData, uuids);
                }
                catch (Exception ex)
                {
                    WriteLineWithPretext($"Updater error for mode {mode}: {ex.Message}", OutputType.Warning);
                    WriteLineWithPretext($"Stack trace: {ex.StackTrace}", OutputType.Warning);
                }

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        });
    }

    /// <summary>
    /// Sends a paginated message
    /// </summary>
    /// <param name="channel">Target channel</param>
    /// <param name="embeds">List of embeds to paginate</param>
    /// <param name="serverUuidStart">UUID of the Server that determines if the server can be started</param>
    /// <param name="serverUuidStop">UUID of the Server that determines if the server can be stopped</param>
    /// <returns>The discord message</returns>
    private static async Task<DiscordMessage> SendPaginatedMessageAsync(this DiscordChannel channel, List<DiscordEmbed> embeds, string? serverUuidStart = null, string? serverUuidStop = null)
    {
        List<DiscordComponent> buttons =
        [
            new DiscordButtonComponent(ButtonStyle.Primary, "prev_page", "◀️ Previous")
        ];
        if (serverUuidStart != null)
        {
            buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary,$"Start: {serverUuidStart}", "Start"));
            if (serverUuidStop != null)
                buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"Stop: {serverUuidStop}", "Stop"));
        }
        buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, "next_page", "Next ▶️"));

        var messageBuilder = new DiscordMessageBuilder()
            .WithEmbed(embeds[0])
            .AddComponents(buttons);

        var message = await messageBuilder.SendAsync(channel);

        LiveMessageStorage.Save(message.Id, 0);

        return message;
    }

}