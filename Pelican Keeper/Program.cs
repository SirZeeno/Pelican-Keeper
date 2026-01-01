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

public static class Program
{
    internal static List<DiscordChannel?> TargetChannel = null!;
    internal static Secrets Secrets = null!;
    public static Config Config = null!;
    private static readonly EmbedBuilderService EmbedService = new();
    internal static List<DiscordEmbed> EmbedPages = null!;
    internal static List<ServerInfo> GlobalServerInfo = null!;

    private enum EmbedUpdateMode { Consolidated, Paginated, PerServer }

    private static async Task Main()
    {
        TargetChannel = [];
        await FileManager.ReadSecretsFile();
        await FileManager.ReadConfigFile();

        // FIX: Check for empty file and recreate if needed
        var mdPath = FileManager.GetFilePath("MessageMarkdown.txt");
        if (mdPath == string.Empty || new FileInfo(mdPath).Length == 0)
        {
            Console.WriteLine("MessageMarkdown.txt missing or empty. Creating default!");
            await FileManager.CreateMessageMarkdownFile();
        }

        GetGamesToMonitorFileAsync();
        ServerMarkdown.GetMarkdownFileContentAsync();

        if (Config.AutoUpdate) await VersionUpdater.UpdateProgram();

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
        else WriteLineWithPretext("ChannelIds in the Secrets File is empty or not spelled correctly!", OutputType.Error);
    }

    private static Task StartStatsUpdater(DiscordClient client, ulong[] channelIds)
    {
        // Consolidated Mode (Dropdowns)
        if (Config.MessageFormat == MessageFormat.Consolidated)
        {
            StartEmbedUpdaterLoop(EmbedUpdateMode.Consolidated,
                async () =>
                {
                    var serversList = GetServersList();
                    GlobalServerInfo = serversList;
                    if (serversList.Count == 0) WriteLineWithPretext("No servers found on Pelican.", OutputType.Error);
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
                                if (Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false } or { AllowUserServerStopping: true })
                                {
                                    List<string?> selectedServerUuids = uuids;
                                    // Filtering based on allowed lists would go here...
                                    List<DiscordComponent> buttons = [];
                                    if (Config.AllowUserServerStartup)
                                    {
                                        var startOptions = selectedServerUuids.Select((uuid, i) => new DiscordSelectComponentOption(label: GlobalServerInfo[i].Name, value: uuid));
                                        foreach (var group in Chunk(startOptions, 25))
                                            buttons.Add(new DiscordSelectComponent("start_menu", "Start a server…", group));
                                    }
                                    if (Config.AllowUserServerStopping)
                                    {
                                        var stopOptions = selectedServerUuids.Select((uuid, i) => new DiscordSelectComponentOption(label: GlobalServerInfo[i].Name, value: uuid));
                                        foreach (var group in Chunk(stopOptions, 25))
                                            buttons.Add(new DiscordSelectComponent("stop_menu", "Stop a server…", group));
                                    }
                                    await msg.ModifyAsync(mb => { mb.WithEmbed(embed); mb.AddRows(buttons); });
                                }
                                else { await msg.ModifyAsync(mb => { mb.WithEmbed(embed); mb.ClearComponents(); }); }
                            }
                            else if (!Config.DryRun)
                            {
                                // Create new message (Consolidated)
                                if (Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false } or { AllowUserServerStopping: true })
                                {
                                    List<string?> selectedServerUuids = uuids;
                                    List<DiscordComponent> buttons = [];
                                    if (Config.AllowUserServerStartup)
                                    {
                                        var startOptions = selectedServerUuids.Select((uuid, i) => new DiscordSelectComponentOption(label: GlobalServerInfo[i].Name, value: uuid));
                                        foreach (var group in Chunk(startOptions, 25)) buttons.Add(new DiscordSelectComponent("start_menu", "Start a server…", group));
                                    }
                                    if (Config.AllowUserServerStopping)
                                    {
                                        var stopOptions = selectedServerUuids.Select((uuid, i) => new DiscordSelectComponentOption(label: GlobalServerInfo[i].Name, value: uuid));
                                        foreach (var group in Chunk(stopOptions, 25)) buttons.Add(new DiscordSelectComponent("stop_menu", "Stop a server…", group));
                                    }
                                    var msg = await channel.SendMessageAsync(mb => { mb.WithEmbed(embed); mb.AddRows(buttons); });
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
                }, Config.ServerUpdateInterval + Random.Shared.Next(0, Config.ServerUpdateInterval / 2)
            );
        }

        // Paginated Mode
        if (Config.MessageFormat == MessageFormat.Paginated)
        {
            StartEmbedUpdaterLoop(EmbedUpdateMode.Paginated,
                async () =>
                {
                    var serversList = GetServersList();
                    GlobalServerInfo = serversList;
                    var uuids = serversList.Select(s => s.Uuid).ToList();
                    var embeds = await EmbedService.BuildPaginatedServerEmbeds(serversList);
                    return (uuids, embeds)!;
                },
                async (embedObj, uuids) =>
                {
                    var embeds = (List<DiscordEmbed>)embedObj;
                    // ... (Logic for Pagination updates)
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
                                var currentIndex = pagedTracked.Value;
                                var updatedEmbed = embeds[currentIndex];
                                var msg = await channel.GetMessageAsync(cacheEntry.Key);
                                if (EmbedHasChanged(uuids, updatedEmbed)) await msg.ModifyAsync(updatedEmbed);
                            }
                            else if (!Config.DryRun)
                            {
                                bool showStart = Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false };
                                bool showStop = Config is { AllowUserServerStopping: true };
                                var uuid = uuids[0];
                                var msg = await channel.SendPaginatedMessageAsync(embeds, showStart ? uuid : null, showStop ? uuid : null);
                                LiveMessageStorage.Save(msg.Id, 0);
                            }
                        }
                    }
                }, Config.ServerUpdateInterval + Random.Shared.Next(0, Config.ServerUpdateInterval / 2)
            );
        }

        // PerServer Mode (Buttons)
        if (Config.MessageFormat == MessageFormat.PerServer)
        {
            var serversList = GetServersList();
            GlobalServerInfo = serversList;
            if (serversList.Count == 0) { WriteLineWithPretext("No servers found on Pelican.", OutputType.Error); return Task.CompletedTask; }
            foreach (var server in serversList)
            {
                StartEmbedUpdaterLoop(EmbedUpdateMode.PerServer,
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

                                bool showStart = Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false };
                                bool showStop = Config is { AllowUserServerStopping: true };

                                if (tracked != null && tracked != 0 && !Config.DryRun)
                                {
                                    var msg = await channel.GetMessageAsync((ulong)tracked);
                                    await msg.ModifyAsync(mb =>
                                    {
                                        mb.WithEmbed(embed);
                                        mb.ClearComponents();
                                        // FIX: Added "start:" and "stop:" prefixes
                                        if (showStart) mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"start:{uuid[0]}", "Start"));
                                        if (showStop) mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"stop:{uuid[0]}", "Stop"));
                                    });
                                }
                                else if (!Config.DryRun)
                                {
                                    var msg = await channel.SendMessageAsync(mb =>
                                    {
                                        mb.WithEmbed(embed);
                                        // FIX: Added "start:" and "stop:" prefixes
                                        if (showStart) mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"start:{uuid[0]}", "Start"));
                                        if (showStop) mb.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"stop:{uuid[0]}", "Stop"));
                                    });
                                    LiveMessageStorage.Save(msg.Id);
                                }
                            }
                        }
                    }, Config.ServerUpdateInterval + Random.Shared.Next(0, 3)
                );
            }
        }
        return Task.CompletedTask;
    }

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
                    WriteLineWithPretext($"Updater error: {ex.Message}", OutputType.Warning);
                }
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        });
    }

    private static async Task<DiscordMessage> SendPaginatedMessageAsync(this DiscordChannel channel, List<DiscordEmbed> embeds, string? serverUuidStart = null, string? serverUuidStop = null)
    {
        List<DiscordComponent> buttons = [new DiscordButtonComponent(ButtonStyle.Primary, "prev_page", "◀️ Previous")];
        // FIX: Standardized start/stop IDs
        if (serverUuidStart != null) buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"start:{serverUuidStart}", "Start"));
        if (serverUuidStop != null) buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"stop:{serverUuidStop}", "Stop"));
        buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, "next_page", "Next ▶️"));
        var message = await new DiscordMessageBuilder().WithEmbed(embeds[0]).AddComponents(buttons).SendAsync(channel);
        LiveMessageStorage.Save(message.Id, 0);
        return message;
    }
}