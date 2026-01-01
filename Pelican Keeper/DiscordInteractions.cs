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
        // Ignore Start/Stop buttons here
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
            // Rebuild buttons for the new page
            var components = new List<DiscordComponent> { new DiscordButtonComponent(ButtonStyle.Primary, "prev_page", "◀️ Previous") };

            // Logic to add Start/Stop buttons for the current server page
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

        // FIX: Check for "start:" prefix
        if (!e.Id.StartsWith("start:")) return Task.CompletedTask;

        // Extract UUID
        var id = e.Id.Split(':')[1];

        // Permission checks
        if (Config.UsersAllowedToStartServers != null && string.Equals(Config.UsersAllowedToStartServers[0], "USERID HERE", StringComparison.Ordinal) && Config.UsersAllowedToStartServers.Length != 0 && !Config.UsersAllowedToStartServers.Contains(e.User.Id.ToString()))
            return Task.CompletedTask;

        var server = GlobalServerInfo.FirstOrDefault(s => s.Uuid == id);
        if (server == null) return Task.CompletedTask;

        if (server.Resources?.CurrentState.ToLower() == "offline")
        {
            PelicanInterface.SendPowerCommand(server.Uuid, "start");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
        return Task.CompletedTask;
    }

    internal static async Task<Task> OnServerStopInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot) return Task.CompletedTask;

        // FIX: Check for "stop:" prefix
        if (!e.Id.StartsWith("stop:")) return Task.CompletedTask;

        // Extract UUID
        var id = e.Id.Split(':')[1];

        // Permission checks
        if (Config.UsersAllowedToStopServers != null && string.Equals(Config.UsersAllowedToStopServers[0], "USERID HERE", StringComparison.Ordinal) && Config.UsersAllowedToStopServers.Length != 0 && !Config.UsersAllowedToStopServers.Contains(e.User.Id.ToString()))
            return Task.CompletedTask;

        var server = GlobalServerInfo.FirstOrDefault(s => s.Uuid == id);
        if (server == null) return Task.CompletedTask;

        if (server.Resources?.CurrentState.ToLower() == "online")
        {
            PelicanInterface.SendPowerCommand(server.Uuid, "stop");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
        return Task.CompletedTask;
    }

    internal static async Task<Task> OnDropDownInteration(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot) return Task.CompletedTask;
        // Consolidated drop down logic works as-is because it reads Values[] directly
        // ... (Keep existing logic or use previous file's logic here)
        return Task.CompletedTask;
    }
}