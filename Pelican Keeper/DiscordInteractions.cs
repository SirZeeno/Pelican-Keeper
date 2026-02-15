using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Pelican_Keeper.Update_Loop_Structures;

namespace Pelican_Keeper;

using static ConsoleExt;
using static Program;

public static class DiscordInteractions
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
            WriteLine($"Live message {e.Message.Id} deleted in channel {e.Message.Channel.Name}. Removing from storage.", CurrentStep.MessageHistory, OutputType.Debug);
            LiveMessageStorage.Remove(liveMessageTracked);
        }
        else if (liveMessageTracked == null)
        {
            var paginatedMessageTracked = LiveMessageStorage.GetPaginated(e.Message.Id);
            if (paginatedMessageTracked == null) return Task.CompletedTask;
            WriteLine($"Paginated message {e.Message.Id} deleted in channel {e.Message.Channel.Name}. Removing from storage.", CurrentStep.MessageHistory, OutputType.Debug);
            LiveMessageStorage.Remove(e.Message.Id);
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
        if (e.Id != "next_page" && e.Id != "prev_page")
        {
            WriteLine("Not a Page Flip Interaction!", CurrentStep.DiscordInteraction, OutputType.Warning);
            return Task.CompletedTask;
        }
        
        if (e.User.IsBot)
        {
            WriteLine("User is a Bot!", CurrentStep.DiscordInteraction, OutputType.Warning);
            return Task.CompletedTask;
        }
        
        if (LiveMessageStorage.GetPaginated(e.Message.Id) is not { } pageTracked)
        {
            WriteLine($"Message ID is not tracked or null. Message ID: {e.Message.Id}", CurrentStep.DiscordMessage, OutputType.Warning);
            return Task.CompletedTask;
        }

        if (EmbedPages.Count == 0)
        {
            WriteLine("EmbedPages is zero, if you just started the bot, wait for the first refresh to be done.", CurrentStep.DiscordInteraction, OutputType.Warning);
            return Task.CompletedTask;
        }

        if (pageTracked >= EmbedPages.Count)
        {
            WriteLine($"Index out of range, page tracked index {pageTracked} is bigger than embed count {EmbedPages.Count}. Using the last index of the current embed pages",  CurrentStep.DiscordInteraction, OutputType.Warning);
            pageTracked = EmbedPages.Count - 1;
        }

        int index = pageTracked;

        WriteLine($"Before Page flip index: {index}", CurrentStep.DiscordInteraction, OutputType.Debug);
        switch (e.Id)
        {
            case "next_page":
                index = (index + 1) % EmbedPages.Count;
                WriteLine($"Next Page index: {index}", CurrentStep.DiscordInteraction, OutputType.Debug);
                break;
            case "prev_page":
                index = (index - 1 + EmbedPages.Count) % EmbedPages.Count;
                WriteLine($"Previous Page index: {index}", CurrentStep.DiscordInteraction, OutputType.Debug);
                break;
            default:
                WriteLine("Unknown interaction ID: " + e.Id, CurrentStep.DiscordInteraction, OutputType.Warning);
                return Task.CompletedTask;
        }

        LiveMessageStorage.Save(e.Message.Id, index);
                
        if (EmbedPages.Count == 0 || pageTracked >= EmbedPages.Count)
        {
            WriteLine("No pages to show or page index out of range", CurrentStep.DiscordInteraction, OutputType.Warning);
            return Task.CompletedTask;
        }

        try
        {
            List<string> uuids = GlobalServerInfo.Select(x => x.Uuid).ToList();

            var components = ButtonCreation.PaginatedButtonCreation(uuids!, index);
            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(EmbedPages[index])
                    .AddComponents(components)
            );
        }
        catch (NotFoundException nf)
        {
            WriteLine("Interaction expired or already responded to. Skipping. " + nf.Message, CurrentStep.DiscordInteraction, OutputType.Error);
        }
        catch (BadRequestException br)
        {
            WriteLine("Bad request during interaction: " + br.JsonMessage, CurrentStep.DiscordInteraction, OutputType.Error);
        }
        catch (Exception ex)
        {
            WriteLine("Unexpected error during component interaction: " + ex.Message, CurrentStep.DiscordInteraction, OutputType.Error);
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
        if (!e.Id.Contains("start", StringComparison.CurrentCultureIgnoreCase) || e.Id == "start_menu")
        {
            return Task.CompletedTask;
        }
        
        if (e.User.IsBot)
        {
            WriteLine("User is a Bot!", CurrentStep.DiscordInteraction, OutputType.Warning);
            return Task.CompletedTask;
        }

        if (Config.UsersAllowedToStartServers != null && !string.Equals(Config.UsersAllowedToStartServers[0], "USERIDS HERE", StringComparison.Ordinal) && Config.UsersAllowedToStartServers.Length != 0 && !Config.UsersAllowedToStartServers.Contains(e.User.Id.ToString()))
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("⛔ You are not allowed to start servers.").AsEphemeral());
            return Task.CompletedTask;
        }

        WriteLine("User " + e.User.Username + " clicked button with ID: " + e.Id, CurrentStep.DiscordInteraction, OutputType.Debug);
        
        var id = e.Id.Split(' ', 2).Last();
        
        var server = GlobalServerInfo.FirstOrDefault(s => s.Uuid == id);
        if (server == null)
        {
            WriteLine($"No server found with UUID {id}", CurrentStep.DiscordInteraction, OutputType.Warning);
            return Task.CompletedTask;
        }
        
        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        switch (server.Resources?.CurrentState.ToLower())
        {
            case "offline":
                PelicanInterface.SendPowerCommand(server.Uuid, "start");

                await e.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"▶️ Starting server `{server.Name}`…")
                        .AsEphemeral()
                );
                WriteLine($"Starting server `{server.Name}`");
                break;
            case "stopping":
                await e.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"▶️ Server `{server.Name}` is stopping, wait for it to stop before starting it up…")
                        .AsEphemeral()
                );
                WriteLine($"Server `{server.Name}` is stopping, wait for it to stop before starting it up");
                break;
            default:
                await e.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"▶️ Server already Running `{server.Name}`…")
                        .AsEphemeral()
                );
                WriteLine($"Server already Running `{server.Name}`");
                break;
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
        if (!e.Id.Contains("stop", StringComparison.CurrentCultureIgnoreCase) || e.Id == "stop_menu")
        {
            return Task.CompletedTask;
        }
        
        if (e.User.IsBot)
        {
            WriteLine("User is a Bot!", CurrentStep.DiscordInteraction, OutputType.Warning);
            return Task.CompletedTask;
        }
        
        if (Config.UsersAllowedToStopServers != null && !string.Equals(Config.UsersAllowedToStopServers[0], "USERIDS HERE", StringComparison.Ordinal) && Config.UsersAllowedToStopServers.Length != 0 && !Config.UsersAllowedToStopServers.Contains(e.User.Id.ToString()))
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("⛔ You are not allowed to stop servers.").AsEphemeral());
            return Task.CompletedTask;
        }

        WriteLine("User " + e.User.Username + " clicked button with ID: " + e.Id, CurrentStep.DiscordInteraction, OutputType.Debug);
        
        var id = e.Id.Split(' ', 2).Last();
        
        var server = GlobalServerInfo.FirstOrDefault(s => s.Uuid == id);
        if (server == null)
        {
            WriteLine($"No server found with UUID {id}", CurrentStep.DiscordInteraction, OutputType.Warning);
            return Task.CompletedTask;
        }
        
        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        switch (server.Resources?.CurrentState.ToLower())
        {
            case "online":
                PelicanInterface.SendPowerCommand(server.Uuid, "stop");

                await e.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"⏹ Stopping server `{server.Name}`…")
                        .AsEphemeral()
                );
                WriteLine($"Stopping server `{server.Name}`");
                break;
            case "starting":
                await e.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"⏹ Server `{server.Name}` is starting, wait for it to start before shutting it down…")
                        .AsEphemeral()
                );
                WriteLine($"Server `{server.Name}` is starting, wait for it to start before shutting it down");
                break;
            default:
                await e.Interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent($"⏹ Server already Stopped `{server.Name}`…")
                        .AsEphemeral()
                );
                WriteLine($"Server already Stopped `{server.Name}`");
                break;
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
        if (e.User.IsBot)
        {
            WriteLine("User is a Bot!", CurrentStep.DiscordInteraction, OutputType.Warning);
            return Task.CompletedTask;
        }
            
        switch (e.Id) //The Identifier of the dropdown
        {
            case "start_menu":
            {
                var uuid = e.Values.FirstOrDefault();
                if (string.IsNullOrEmpty(uuid)) return Task.CompletedTask;
                
                var serverInfo = GlobalServerInfo.FirstOrDefault(x => x.Uuid == uuid);
                if (serverInfo == null)
                {
                    WriteLine($"No server found with UUID {uuid}", CurrentStep.DiscordInteraction, OutputType.Warning);
                    return Task.CompletedTask;
                }
                
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                        
                switch (serverInfo.Resources?.CurrentState.ToLower())
                {
                    case "offline":
                        PelicanInterface.SendPowerCommand(serverInfo.Uuid, "start");

                        await e.Interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder()
                                .WithContent($"▶️ Starting server `{serverInfo.Name}`…")
                                .AsEphemeral()
                        );
                        WriteLine($"Starting server `{serverInfo.Name}`");
                        break;
                    case "stopping":
                        await e.Interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder()
                                .WithContent($"▶️ Server `{serverInfo.Name}` is stopping, wait for it to stop before starting it up…")
                                .AsEphemeral()
                        );
                        WriteLine($"Server `{serverInfo.Name}` is stopping, wait for it to stop before starting it up");
                        break;
                    default:
                        await e.Interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder()
                                .WithContent($"▶️ Server already Running `{serverInfo.Name}`…")
                                .AsEphemeral()
                        );
                        WriteLine($"Server already Running `{serverInfo.Name}`");
                        break;
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

                    switch (serverInfo.Resources?.CurrentState.ToLower())
                    {
                        case "online":
                            PelicanInterface.SendPowerCommand(serverInfo.Uuid, "stop");

                            await e.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder()
                                    .WithContent($"⏹ Stopping server `{serverInfo.Name}`…")
                                    .AsEphemeral()
                            );
                            WriteLine($"Stopping server `{serverInfo.Name}`");
                            break;
                        case "starting":
                            await e.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder()
                                    .WithContent($"⏹ Server `{serverInfo.Name}` is starting, wait for it to start before shutting it down…")
                                    .AsEphemeral()
                            );
                            WriteLine($"Server `{serverInfo.Name}` is starting, wait for it to start before shutting it down");
                            break;
                        default:
                            await e.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder()
                                    .WithContent($"⏹ Server already Stopped `{serverInfo.Name}`…")
                                    .AsEphemeral()
                            );
                            WriteLine($"Server already Stopped `{serverInfo.Name}`");
                            break;
                    }
                }
                break;
            }
        }
        return Task.CompletedTask;
    }
}