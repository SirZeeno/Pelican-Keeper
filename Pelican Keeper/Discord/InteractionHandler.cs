using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Pelican_Keeper.Core;
using Pelican_Keeper.Pelican;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Discord;

/// <summary>
/// Handles Discord button and dropdown interactions.
/// </summary>
public static class InteractionHandler
{
    /// <summary>
    /// Handles message deletion to clean up tracked message IDs.
    /// </summary>
    public static Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
    {
        if (RuntimeContext.Secrets.ChannelIds?.Contains(e.Message.Id) == true)
            return Task.CompletedTask;

        var tracked = LiveMessageStorage.Get(e.Message.Id);
        if (tracked != null)
            LiveMessageStorage.Remove(tracked);
        else if (LiveMessageStorage.GetPaginated(e.Message.Id) != null)
            LiveMessageStorage.Remove(e.Message.Id);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles pagination button clicks.
    /// </summary>
    public static async Task<Task> OnPageFlipInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.Id.StartsWith("start:") || e.Id.StartsWith("stop:"))
            return Task.CompletedTask;

        if (LiveMessageStorage.GetPaginated(e.Message.Id) is not { } pageIndex || e.User.IsBot)
            return Task.CompletedTask;

        var newIndex = e.Id switch
        {
            "next_page" => (pageIndex + 1) % RuntimeContext.EmbedPages.Count,
            "prev_page" => (pageIndex - 1 + RuntimeContext.EmbedPages.Count) % RuntimeContext.EmbedPages.Count,
            _ => pageIndex
        };

        if (newIndex == pageIndex) return Task.CompletedTask;

        LiveMessageStorage.Save(e.Message.Id, newIndex);

        try
        {
            var uuid = RuntimeContext.ServerInfoCache[newIndex].Uuid;
            var components = ComponentBuilders.BuildPaginationButtons(uuid, RuntimeContext.Config);

            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(RuntimeContext.EmbedPages[newIndex])
                    .AddComponents(components));
        }
        catch (Exception ex)
        {
            if (RuntimeContext.Config.Debug)
                Logger.WriteLineWithStep($"Page flip error: {ex.Message}", Logger.Step.DiscordInteraction, Logger.OutputType.Error);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles server start button clicks.
    /// </summary>
    public static async Task<Task> OnServerStartInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot || !e.Id.StartsWith("start:"))
            return Task.CompletedTask;

        var uuid = e.Id.Split(':')[1];

        if (!IsUserAuthorizedToStart(e.User.Id.ToString()))
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("⛔ You are not allowed to start servers.")
                    .AsEphemeral());
            return Task.CompletedTask;
        }

        var server = RuntimeContext.ServerInfoCache.FirstOrDefault(s => s.Uuid == uuid);
        if (server == null) return Task.CompletedTask;

        if (server.Resources?.CurrentState.ToLower() == "offline")
        {
            PelicanApiClient.SendPowerCommand(uuid, "start");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
        else
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"⚠️ Server {server.Name} is already running.")
                    .AsEphemeral());
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles server stop button clicks.
    /// </summary>
    public static async Task<Task> OnServerStopInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot || !e.Id.StartsWith("stop:"))
            return Task.CompletedTask;

        var uuid = e.Id.Split(':')[1];

        if (!IsUserAuthorizedToStop(e.User.Id.ToString()))
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("⛔ You are not allowed to stop servers.")
                    .AsEphemeral());
            return Task.CompletedTask;
        }

        var server = RuntimeContext.ServerInfoCache.FirstOrDefault(s => s.Uuid == uuid);
        if (server == null) return Task.CompletedTask;

        if (server.Resources?.CurrentState.ToLower() != "offline")
        {
            PelicanApiClient.SendPowerCommand(uuid, "stop");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
        else
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"⚠️ Server {server.Name} is not online.")
                    .AsEphemeral());
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles dropdown menu selections.
    /// </summary>
    public static async Task<Task> OnDropdownInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot) return Task.CompletedTask;

        var uuid = e.Values.FirstOrDefault();
        if (string.IsNullOrEmpty(uuid)) return Task.CompletedTask;

        var server = RuntimeContext.ServerInfoCache.FirstOrDefault(x => x.Uuid == uuid);
        if (server == null) return Task.CompletedTask;

        PelicanApiClient.GetServerStats(server);

        if (e.Id == "start_menu")
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            if (server.Resources?.CurrentState.ToLower() == "offline")
            {
                PelicanApiClient.SendPowerCommand(uuid, "start");
                await e.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"▶️ Starting `{server.Name}`...")
                        .AsEphemeral());
            }
            else
            {
                await e.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"⚠️ `{server.Name}` is already running (State: {server.Resources?.CurrentState}).")
                        .AsEphemeral());
            }
        }
        else if (e.Id == "stop_menu")
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            if (server.Resources?.CurrentState.ToLower() != "offline")
            {
                PelicanApiClient.SendPowerCommand(uuid, "stop");
                await e.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"⏹ Stopping `{server.Name}`...")
                        .AsEphemeral());
            }
            else
            {
                await e.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"⚠️ `{server.Name}` is already offline.")
                        .AsEphemeral());
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsUserAuthorizedToStart(string userId)
    {
        var allowed = RuntimeContext.Config.UsersAllowedToStartServers;
        if (allowed == null || allowed.Length == 0) return true;
        if (allowed[0].Equals("USERID HERE", StringComparison.OrdinalIgnoreCase)) return true;
        return allowed.Contains(userId);
    }

    private static bool IsUserAuthorizedToStop(string userId)
    {
        var allowed = RuntimeContext.Config.UsersAllowedToStopServers;
        if (allowed == null || allowed.Length == 0) return true;
        if (allowed[0].Equals("USERID HERE", StringComparison.OrdinalIgnoreCase)) return true;
        return allowed.Contains(userId);
    }
}
