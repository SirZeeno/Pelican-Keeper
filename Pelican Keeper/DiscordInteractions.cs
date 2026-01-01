using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;

namespace Pelican_Keeper;

using static ConsoleExt;
using static Program;

public static class DiscordInteractions
{
    internal static Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
    {
        if (Secrets.ChannelIds != null && Secrets.ChannelIds.Contains(e.Message.Id)) return Task.CompletedTask;
        var liveMessageTracked = LiveMessageStorage.Get(e.Message.Id);
        if (liveMessageTracked != null) LiveMessageStorage.Remove(liveMessageTracked);
        else if (LiveMessageStorage.GetPaginated(e.Message.Id) != null) LiveMessageStorage.Remove(e.Message.Id);
        return Task.CompletedTask;
    }

    internal static async Task<Task> OnPageFlipInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.Id.StartsWith("start:") || e.Id.StartsWith("stop:")) return Task.CompletedTask;

        if (LiveMessageStorage.GetPaginated(e.Message.Id) is not { } pagedTracked || e.User.IsBot) return Task.CompletedTask;
        int index = pagedTracked;
        switch (e.Id)
        {
            case "next_page": index = (index + 1) % EmbedPages.Count; break;
            case "prev_page": index = (index - 1 + EmbedPages.Count) % EmbedPages.Count; break;
            default: return Task.CompletedTask;
        }
        LiveMessageStorage.Save(e.Message.Id, index);

        try
        {
            var components = new List<DiscordComponent> { new DiscordButtonComponent(ButtonStyle.Primary, "prev_page", "◀️ Previous") };

            bool showStart = Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false };
            bool showStop = Config is { AllowUserServerStopping: true };
            string uuid = GlobalServerInfo[index].Uuid;

            if (showStart) components.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"start:{uuid}", "Start"));
            if (showStop) components.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"stop:{uuid}", "Stop"));

            components.Add(new DiscordButtonComponent(ButtonStyle.Primary, "next_page", "Next ▶️"));

            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder().AddEmbed(EmbedPages[index]).AddComponents(components)
            );
        }
        catch (Exception ex)
        {
            if (Config.Debug) WriteLineWithStepPretext("Error during page flip: " + ex.Message, CurrentStep.DiscordReaction, OutputType.Error);
        }
        return Task.CompletedTask;
    }

    internal static async Task<Task> OnServerStartInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot) return Task.CompletedTask;
        if (!e.Id.StartsWith("start:")) return Task.CompletedTask;

        var id = e.Id.Split(':')[1];

        bool isRestricted = Config.UsersAllowedToStartServers != null
                            && Config.UsersAllowedToStartServers.Length > 0
                            && !string.Equals(Config.UsersAllowedToStartServers[0], "USERID HERE", StringComparison.OrdinalIgnoreCase);

        if (isRestricted && !Config.UsersAllowedToStartServers!.Contains(e.User.Id.ToString()))
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("⛔ You are not allowed to start servers.").AsEphemeral());
            return Task.CompletedTask;
        }

        var server = GlobalServerInfo.FirstOrDefault(s => s.Uuid == id);
        if (server == null) return Task.CompletedTask;

        if (server.Resources?.CurrentState.ToLower() == "offline")
        {
            PelicanInterface.SendPowerCommand(server.Uuid, "start");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
        else
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
               new DiscordInteractionResponseBuilder().WithContent($"⚠️ Server {server.Name} is already running.").AsEphemeral());
        }
        return Task.CompletedTask;
    }

    internal static async Task<Task> OnServerStopInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot) return Task.CompletedTask;
        if (!e.Id.StartsWith("stop:")) return Task.CompletedTask;

        var id = e.Id.Split(':')[1];

        bool isRestricted = Config.UsersAllowedToStopServers != null
                            && Config.UsersAllowedToStopServers.Length > 0
                            && !string.Equals(Config.UsersAllowedToStopServers[0], "USERID HERE", StringComparison.OrdinalIgnoreCase);

        if (isRestricted && !Config.UsersAllowedToStopServers!.Contains(e.User.Id.ToString()))
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("⛔ You are not allowed to stop servers.").AsEphemeral());
            return Task.CompletedTask;
        }

        var server = GlobalServerInfo.FirstOrDefault(s => s.Uuid == id);
        if (server == null) return Task.CompletedTask;

        if (server.Resources?.CurrentState.ToLower() == "online")
        {
            PelicanInterface.SendPowerCommand(server.Uuid, "stop");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
        else
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
               new DiscordInteractionResponseBuilder().WithContent($"⚠️ Server {server.Name} is not online.").AsEphemeral());
        }
        return Task.CompletedTask;
    }

    internal static async Task<Task> OnDropDownInteration(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot) return Task.CompletedTask;

        string? uuid = e.Values.FirstOrDefault();
        if (string.IsNullOrEmpty(uuid)) return Task.CompletedTask;

        var serverInfo = GlobalServerInfo.FirstOrDefault(x => x.Uuid == uuid);
        if (serverInfo == null) return Task.CompletedTask;

        // FIX: Force Update Server Stats before checking status
        PelicanInterface.GetServerStats(serverInfo);

        if (e.Id == "start_menu")
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            if (serverInfo.Resources?.CurrentState.ToLower() == "offline")
            {
                PelicanInterface.SendPowerCommand(serverInfo.Uuid, "start");
                await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"▶️ Starting `{serverInfo.Name}`...").AsEphemeral());
            }
            else
            {
                await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"⚠️ `{serverInfo.Name}` is already running (State: {serverInfo.Resources?.CurrentState}).").AsEphemeral());
            }
        }
        else if (e.Id == "stop_menu")
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            // Allow stopping if state is anything other than offline (e.g. starting, running)
            if (serverInfo.Resources?.CurrentState.ToLower() != "offline")
            {
                PelicanInterface.SendPowerCommand(serverInfo.Uuid, "stop");
                await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"⏹ Stopping `{serverInfo.Name}`...").AsEphemeral());
            }
            else
            {
                await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"⚠️ `{serverInfo.Name}` is already offline.").AsEphemeral());
            }
        }

        return Task.CompletedTask;
    }
}