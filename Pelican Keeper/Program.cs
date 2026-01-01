using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Pelican_Keeper.Configuration;
using Pelican_Keeper.Core;
using Pelican_Keeper.Discord;
using Pelican_Keeper.Pelican;
using Pelican_Keeper.Updates;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper;

/// <summary>
/// Application entry point and main event loop.
/// </summary>
public static class Program
{
    private static readonly EmbedBuilderService EmbedService = new();
    private static DiscordClient? _discordClient;

    /// <summary>
    /// Main application entry point.
    /// </summary>
    public static async Task Main()
    {
        Logger.WriteLineWithStep("Starting Pelican Keeper...", Logger.Step.Initialization);

        await InitializeConfigurationAsync();
        await InitializeServicesAsync();
        await CheckForUpdatesAsync();
        await StartDiscordClientAsync();
    }

    private static async Task InitializeConfigurationAsync()
    {
        await FileManager.ReadSecretsFileAsync();
        await FileManager.ReadConfigFileAsync();
        await EnsureMarkdownFileAsync();
        await ServerMonitorService.InitializeAsync();
    }

    private static async Task EnsureMarkdownFileAsync()
    {
        var mdPath = FileManager.GetFilePath("MessageMarkdown.txt");
        if (mdPath == string.Empty || new FileInfo(mdPath).Length == 0)
        {
            Logger.WriteLineWithStep("MessageMarkdown.txt missing. Creating default.", Logger.Step.FileReading);
            await FileManager.CreateMessageMarkdownFileAsync();
        }
    }

    private static async Task InitializeServicesAsync()
    {
        await ServerMarkdownParser.LoadTemplateAsync();

        if (RuntimeContext.Config.ContinuesMarkdownRead)
            ServerMarkdownParser.StartContinuousReload();

        if (RuntimeContext.Config.ContinuesGamesToMonitorRead)
            ServerMonitorService.StartContinuousGamesReload();
    }

    private static async Task CheckForUpdatesAsync()
    {
        if (RuntimeContext.Config.AutoUpdate || RuntimeContext.Config.NotifyOnUpdate || VersionUpdater.IsAutoUpdateEnabled())
        {
            await VersionUpdater.CheckForUpdatesAsync();
        }
    }

    private static async Task StartDiscordClientAsync()
    {
        _discordClient = new DiscordClient(new DiscordConfiguration
        {
            Token = RuntimeContext.Secrets.BotToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
        });

        _discordClient.Ready += OnClientReady;
        _discordClient.MessageDeleted += InteractionHandler.OnMessageDeleted;
        _discordClient.ComponentInteractionCreated += InteractionHandler.OnPageFlipInteraction;
        _discordClient.ComponentInteractionCreated += InteractionHandler.OnServerStartInteraction;
        _discordClient.ComponentInteractionCreated += InteractionHandler.OnServerStopInteraction;
        _discordClient.ComponentInteractionCreated += InteractionHandler.OnDropdownInteraction;

        await _discordClient.ConnectAsync();
        await Task.Delay(-1);
    }

    private static async Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        Logger.WriteLineWithStep("Bot is connected and ready!", Logger.Step.Discord);

        if (RuntimeContext.Secrets.ChannelIds == null || RuntimeContext.Secrets.ChannelIds.Length == 0)
        {
            Logger.WriteLineWithStep("No channel IDs configured in Secrets.", Logger.Step.Discord, Logger.OutputType.Error);
            return;
        }

        foreach (var channelId in RuntimeContext.Secrets.ChannelIds)
        {
            var channel = await sender.GetChannelAsync(channelId);
            if (channel == null)
            {
                Logger.WriteLineWithStep($"Channel {channelId} not found, skipping.", Logger.Step.Discord, Logger.OutputType.Warning);
                continue;
            }
            RuntimeContext.TargetChannels.Add(channel);
            Logger.WriteLineWithStep($"Target channel: {channel.Name}", Logger.Step.Discord);
        }

        // Send update notification now that channels are available
        if (VersionUpdater.UpdateAvailable && RuntimeContext.Config.NotifyOnUpdate)
        {
            await VersionUpdater.SendDiscordNotificationAsync();
        }

        // Handle auto-update after notification
        if (VersionUpdater.UpdateAvailable && (RuntimeContext.Config.AutoUpdate || VersionUpdater.IsAutoUpdateEnabled()))
        {
            await VersionUpdater.PerformUpdateAsync();
        }

        StartStatsUpdater(sender);
        VersionUpdater.StartPeriodicUpdateCheck(TimeSpan.FromHours(24));
    }

    private static void StartStatsUpdater(DiscordClient client)
    {
        var config = RuntimeContext.Config;

        switch (config.MessageFormat)
        {
            case Models.MessageFormat.Consolidated:
                StartConsolidatedUpdater(client);
                break;
            case Models.MessageFormat.Paginated:
                StartPaginatedUpdater(client);
                break;
            case Models.MessageFormat.PerServer:
                StartPerServerUpdater(client);
                break;
        }
    }

    private static void StartConsolidatedUpdater(DiscordClient client)
    {
        StartUpdaterLoop(async () =>
        {
            var servers = ServerMonitorService.GetProcessedServerList();
            RuntimeContext.ServerInfoCache = servers;

            if (servers.Count == 0)
            {
                Logger.WriteLineWithStep("No servers to display.", Logger.Step.EmbedBuilding);
                return;
            }

            var uuids = servers.Select(s => s.Uuid).ToList();
            var embed = await EmbedService.BuildMultiServerEmbedAsync(servers);

            if (!EmbedChangeTracker.HasChanged(uuids, embed)) return;

            foreach (var channel in RuntimeContext.TargetChannels)
            {
                await UpdateOrCreateConsolidatedMessage(channel, embed, servers);
            }
        });
    }

    private static async Task UpdateOrCreateConsolidatedMessage(DiscordChannel channel, DiscordEmbed embed, List<Models.ServerInfo> servers)
    {
        if (RuntimeContext.Config.DryRun) return;

        var messageId = await LiveMessageStorage.GetExistingMessageIdAsync(channel);
        var components = ComponentBuilders.BuildServerDropdowns(servers, RuntimeContext.Config);

        if (messageId.HasValue)
        {
            try
            {
                var msg = await channel.GetMessageAsync(messageId.Value);
                await msg.ModifyAsync(mb =>
                {
                    mb.WithEmbed(embed);
                    mb.ClearComponents();
                    mb.AddComponentRows(components);
                });
            }
            catch
            {
                await CreateNewConsolidatedMessage(channel, embed, components);
            }
        }
        else
        {
            await CreateNewConsolidatedMessage(channel, embed, components);
        }
    }

    private static async Task CreateNewConsolidatedMessage(DiscordChannel channel, DiscordEmbed embed, List<DiscordComponent> components)
    {
        var msg = await new DiscordMessageBuilder()
            .WithEmbed(embed)
            .AddComponentRows(components)
            .SendAsync(channel);

        await LiveMessageStorage.SaveAsync(msg.Id);
    }

    private static void StartPaginatedUpdater(DiscordClient client)
    {
        StartUpdaterLoop(async () =>
        {
            var servers = ServerMonitorService.GetProcessedServerList();
            RuntimeContext.ServerInfoCache = servers;

            if (servers.Count == 0) return;

            var uuids = servers.Select(s => s.Uuid).ToList();
            var embeds = await EmbedService.BuildPaginatedEmbedsAsync(servers);
            RuntimeContext.EmbedPages = embeds;

            foreach (var channel in RuntimeContext.TargetChannels)
            {
                await UpdateOrCreatePaginatedMessage(channel, embeds, servers);
            }
        });
    }

    private static async Task UpdateOrCreatePaginatedMessage(DiscordChannel channel, List<DiscordEmbed> embeds, List<Models.ServerInfo> servers)
    {
        if (RuntimeContext.Config.DryRun) return;

        var (messageId, pageIndex) = await LiveMessageStorage.GetExistingPaginatedMessageAsync(channel);

        if (messageId.HasValue && pageIndex.HasValue)
        {
            try
            {
                var safeIndex = Math.Min(pageIndex.Value, embeds.Count - 1);
                var msg = await channel.GetMessageAsync(messageId.Value);
                var currentUuid = servers.Count > safeIndex ? servers[safeIndex].Uuid : null;

                await msg.ModifyAsync(mb =>
                {
                    mb.WithEmbed(embeds[safeIndex]);
                    mb.ClearComponents();
                    var buttons = ComponentBuilders.BuildPaginationButtons(currentUuid, RuntimeContext.Config);
                    mb.AddComponents(buttons);
                });
            }
            catch
            {
                await CreateNewPaginatedMessage(channel, embeds, servers);
            }
        }
        else
        {
            await CreateNewPaginatedMessage(channel, embeds, servers);
        }
    }

    private static async Task CreateNewPaginatedMessage(DiscordChannel channel, List<DiscordEmbed> embeds, List<Models.ServerInfo> servers)
    {
        var firstUuid = servers.FirstOrDefault()?.Uuid;
        var buttons = ComponentBuilders.BuildPaginationButtons(firstUuid, RuntimeContext.Config);

        var msg = await new DiscordMessageBuilder()
            .WithEmbed(embeds[0])
            .AddComponents(buttons)
            .SendAsync(channel);

        await LiveMessageStorage.SaveAsync(msg.Id, 0);
    }

    private static void StartPerServerUpdater(DiscordClient client)
    {
        var servers = ServerMonitorService.GetProcessedServerList();
        RuntimeContext.ServerInfoCache = servers;

        if (servers.Count == 0)
        {
            Logger.WriteLineWithStep("No servers found for PerServer mode.", Logger.Step.EmbedBuilding);
            return;
        }

        foreach (var server in servers)
        {
            StartUpdaterLoop(async () =>
            {
                var embed = await EmbedService.BuildSingleServerEmbedAsync(server);

                if (!EmbedChangeTracker.HasChanged([server.Uuid], embed)) return;

                foreach (var channel in RuntimeContext.TargetChannels)
                {
                    await UpdateOrCreateServerMessage(channel, embed, server);
                }
            }, RuntimeContext.Config.ServerUpdateInterval + Random.Shared.Next(0, 3));
        }
    }

    private static async Task UpdateOrCreateServerMessage(DiscordChannel channel, DiscordEmbed embed, Models.ServerInfo server)
    {
        if (RuntimeContext.Config.DryRun) return;

        var messageId = await LiveMessageStorage.GetExistingMessageIdAsync(channel);
        var buttons = ComponentBuilders.BuildServerButtons(server.Uuid, RuntimeContext.Config);

        if (messageId.HasValue)
        {
            try
            {
                var msg = await channel.GetMessageAsync(messageId.Value);
                await msg.ModifyAsync(mb =>
                {
                    mb.WithEmbed(embed);
                    mb.ClearComponents();
                    if (buttons.Count > 0) mb.AddComponents(buttons);
                });
            }
            catch
            {
                await CreateNewServerMessage(channel, embed, buttons);
            }
        }
        else
        {
            await CreateNewServerMessage(channel, embed, buttons);
        }
    }

    private static async Task CreateNewServerMessage(DiscordChannel channel, DiscordEmbed embed, List<DiscordButtonComponent> buttons)
    {
        var builder = new DiscordMessageBuilder().WithEmbed(embed);
        if (buttons.Count > 0) builder.AddComponents(buttons);

        var msg = await builder.SendAsync(channel);
        await LiveMessageStorage.SaveAsync(msg.Id);
    }

    private static int CalculateUpdateDelaySeconds(int? delaySeconds)
    {
        return delaySeconds
               ?? RuntimeContext.Config.ServerUpdateInterval
               + Random.Shared.Next(0, RuntimeContext.Config.ServerUpdateInterval / 2);
    }

    private static void StartUpdaterLoop(Func<Task> updateAction, int? delaySeconds = null)
    {
        var delay = CalculateUpdateDelaySeconds(delaySeconds);

        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await updateAction();
                }
                catch (Exception ex)
                {
                    Logger.WriteLineWithStep($"Updater error: {ex.Message}", Logger.Step.EmbedBuilding, Logger.OutputType.Warning);
                }

                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        });
    }
}
