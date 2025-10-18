using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;

namespace Pelican_Keeper;

using static ConsoleExt;
using static Program;

public class DiscordInteractions
{
    /// <summary>
    /// Function that is called when a message is deleted in the target channel.
    /// </summary>
    /// <param name="sender">DiscordClient</param>
    /// <param name="e">MessageDeleteEventArgs</param>
    /// <returns>Task</returns>
    internal static Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
    {
        if (Secrets.ChannelIds != null && Secrets.ChannelIds.Contains(e.Message.Id)) return Task.CompletedTask;

        var liveMessageTracked = LiveMessageStorage.Get(e.Message.Id);
        if (liveMessageTracked != null)
        {
            if (Config.Debug)
                WriteLineWithPretext($"Live message {e.Message.Id} deleted in channel {e.Message.Channel.Name}. Removing from storage.");
            LiveMessageStorage.Remove(liveMessageTracked);
        }
        else if (liveMessageTracked == null)
        {
            var paginatedMessageTracked = LiveMessageStorage.GetPaginated(e.Message.Id);
            if (paginatedMessageTracked != null)
            {
                if (Config.Debug)
                    WriteLineWithPretext($"Paginated message {e.Message.Id} deleted in channel {e.Message.Channel.Name}. Removing from storage.");
                LiveMessageStorage.Remove(e.Message.Id);
            }
        }
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Function that is called when a component interaction is created.
    /// It handles the page flipping of the paginated message using buttons.
    /// </summary>
    /// <param name="sender">DiscordClient</param>
    /// <param name="e">ComponentInteractionCreateEventArgs</param>
    /// <returns>Task of Type Task</returns>
    internal static async Task<Task> OnPageFlipInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (LiveMessageStorage.GetPaginated(e.Message.Id) is not { } pagedTracked || e.User.IsBot)
        {
            if (Config.Debug)
                WriteLineWithPretext("User is Bot or is not tracked, message ID is null.", ConsoleExt.OutputType.Warning);
            return Task.CompletedTask;
        }

        int index = pagedTracked;

        switch (e.Id)
        {
            case "next_page":
                index = (index + 1) % EmbedPages.Count;
                break;
            case "prev_page":
                index = (index - 1 + EmbedPages.Count) % EmbedPages.Count;
                break;
            default:
                if (Config.Debug)
                    WriteLineWithPretext("Unknown interaction ID: " + e.Id, ConsoleExt.OutputType.Warning);
                return Task.CompletedTask;
        }

        LiveMessageStorage.Save(e.Message.Id, index);
                
        if (EmbedPages.Count == 0 || pagedTracked >= EmbedPages.Count)
        {
            WriteLineWithPretext("No pages to show or page index out of range", ConsoleExt.OutputType.Warning);
            return Task.CompletedTask;
        }

        try
        {
            // treat "UUIDS HERE" placeholder or empty/null list as "allow all"
            bool allowAllStart = Config.AllowServerStartup == null || Config.AllowServerStartup.Length == 0 || string.Equals(Config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal);
            WriteLineWithPretext("show all Start: " + allowAllStart);

            // allow only if user-startup enabled, not ignoring offline, and either allow-all or in allow-list
            bool showStart = Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false, AllowServerStartup: not null } && (allowAllStart || Config.AllowServerStartup.Contains(GlobalServerInfo[index].Uuid, StringComparer.OrdinalIgnoreCase));
            WriteLineWithPretext("show Start: " + showStart);
            
            // treat "UUIDS HERE" placeholder or empty/null list as "allow all"
            bool allowAllStop = Config.AllowServerStopping == null || Config.AllowServerStopping.Length == 0 || string.Equals(Config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal);
            WriteLineWithPretext("show all Stop: " + allowAllStop);

            // allow only if user-startup enabled, not ignoring offline, and either allow-all or in stop-list
            bool showStop = Config is { AllowUserServerStopping: true, AllowServerStopping: not null } && (allowAllStop || Config.AllowServerStopping.Contains(GlobalServerInfo[index].Uuid, StringComparer.OrdinalIgnoreCase));
            WriteLineWithPretext("show Stop: " + showStop);

            var components = new List<DiscordComponent>
            {
                new DiscordButtonComponent(ButtonStyle.Primary, "prev_page", "◀️ Previous")
            };
            if (showStart)
            {
                components.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"Start: {GlobalServerInfo[index].Uuid}", $"Start"));
            }
            if (showStop)
            {
                components.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"Stop: {GlobalServerInfo[index].Uuid}", $"Stop"));
            }
            components.Add(new DiscordButtonComponent(ButtonStyle.Primary, "next_page", "Next ▶️"));
            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(EmbedPages[index])
                    .AddComponents(components)
            );
        }
        catch (NotFoundException nf)
        {
            if (Config.Debug)
                WriteLineWithPretext("Interaction expired or already responded to. Skipping. " + nf.Message, ConsoleExt.OutputType.Error);
        }
        catch (BadRequestException br)
        {
            if (Config.Debug)
                WriteLineWithPretext("Bad request during interaction: " + br.JsonMessage, ConsoleExt.OutputType.Error);
        }
        catch (Exception ex)
        {
            if (Config.Debug)
                WriteLineWithPretext("Unexpected error during component interaction: " + ex.Message, ConsoleExt.OutputType.Error);
        }

        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Function that is called when the user interacts with the Start Button. It sends a start command to the Server in question
    /// </summary>
    /// <param name="sender">DiscordClient</param>
    /// <param name="e">MessageDeleteEventArgs</param>
    /// <returns>Task of Type Task</returns>
    internal static async Task<Task> OnServerStartInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot)
        {
            if (Config.Debug)
                WriteLineWithPretext("User is a Bot!", OutputType.Warning);
            return Task.CompletedTask;
        }

        if (!e.Id.ToLower().Contains("start") || Config.UsersAllowedToStartServers != null && string.Equals(Config.UsersAllowedToStartServers[0], "USERID HERE", StringComparison.Ordinal) && Config.UsersAllowedToStartServers.Length != 0 && !Config.UsersAllowedToStartServers.Contains(e.User.Id.ToString()))
        {
            return Task.CompletedTask;
        }

        if (Config.Debug)
            WriteLineWithPretext("User " + e.User.Username + " clicked button with ID: " + e.Id);
        
        var id = e.Id;
        var server = GlobalServerInfo.FirstOrDefault(s => s.Uuid == id);
        if (server == null)
        {
            if (Config.Debug)
                WriteLineWithPretext($"No server found with UUID {id}", OutputType.Warning);
            return Task.CompletedTask;
        }

        if (server.Resources?.CurrentState.ToLower() == "offline")
        {
            PelicanInterface.SendPowerCommand(server.Uuid, "start");
            if (Config.Debug)
                WriteLineWithPretext("Start command sent to server " + server.Name);
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Function that is called when the user interacts with the Stop Button. It sends a stop command to the Server in question
    /// </summary>
    /// <param name="sender">DiscordClient</param>
    /// <param name="e">MessageDeleteEventArgs</param>
    /// <returns>Task of Type Task</returns>
    internal static async Task<Task> OnServerStopInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot)
        {
            if (Config.Debug)
                WriteLineWithPretext("User is a Bot!", OutputType.Warning);
            return Task.CompletedTask;
        }
        
        if (!e.Id.ToLower().Contains("stop") || Config.UsersAllowedToStopServers != null && string.Equals(Config.UsersAllowedToStopServers[0], "USERID HERE", StringComparison.Ordinal) && Config.UsersAllowedToStopServers.Length != 0 && !Config.UsersAllowedToStopServers.Contains(e.User.Id.ToString()))
        {
            return Task.CompletedTask;
        }

        if (Config.Debug)
            WriteLineWithPretext("User " + e.User.Username + " clicked button with ID: " + e.Id);
        
        var id = e.Id;
        var server = GlobalServerInfo.FirstOrDefault(s => s.Uuid == id);
        if (server == null)
        {
            if (Config.Debug)
                WriteLineWithPretext($"No server found with UUID {id}", OutputType.Warning);
            return Task.CompletedTask;
        }

        if (server.Resources?.CurrentState.ToLower() == "online")
        {
            PelicanInterface.SendPowerCommand(server.Uuid, "stop");
            if (Config.Debug)
                WriteLineWithPretext("Stop command sent to server " + server.Name);
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Function that is called when the user interacts with the dropdown menu. It starts or stops the server you selected
    /// </summary>
    /// <param name="sender">DiscordClient</param>
    /// <param name="e">MessageDeleteEventArgs</param>
    /// <returns>Task of Type Task</returns>
    internal static async Task<Task> OnDropDownInteration(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User.IsBot) return Task.CompletedTask;
            
        switch (e.Id) //The Identifier of the dropdown
        {
            case "start_menu":
            {
                var uuid = e.Values.FirstOrDefault();
                var serverInfo = GlobalServerInfo.FirstOrDefault(x => x.Uuid == uuid);
                if (serverInfo != null)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                        
                    PelicanInterface.SendPowerCommand(serverInfo.Uuid, "start");
                        
                    await e.Interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent($"▶️ Starting server `{serverInfo.Name}`…")
                            .AsEphemeral()
                    );
                }
                break;
            }
            case "stop_menu":
            {
                var uuid = e.Values.FirstOrDefault();
                var serverInfo = GlobalServerInfo.FirstOrDefault(x => x.Uuid == uuid);
                if (serverInfo != null)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                        
                    PelicanInterface.SendPowerCommand(serverInfo.Uuid, "stop");
                        
                    await e.Interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent($"⏹ Stopping server `{serverInfo.Name}`…")
                            .AsEphemeral()
                    );
                }
                break;
            }
        }
        return Task.CompletedTask;
    }
}