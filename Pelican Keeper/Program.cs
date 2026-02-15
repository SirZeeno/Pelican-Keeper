using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Pelican_Keeper.Update_Loop_Structures;

namespace Pelican_Keeper;

using static TemplateClasses;
using static PelicanInterface;
using static ConsoleExt;
using static DiscordInteractions;

/// <summary>
/// Changelog
///
/// V2.0.4
/// Added Previous Message checks to the cache validation to try and get the previous message in the target channel if none of the messages in the cache exist.
/// Fixed Redundant double checks to the message history cache
/// Split each message update loop into its own class
/// Added the Version parameter into the project config and into the Update checker.
/// Added OutputMode into the config to further narrow down what debug levels should be written into console
/// Added an option that allows the bypass of the debug config check for console outputs
/// Changed the Default for Debug mode to false
///
/// V2.0.5
/// Added the OutputMode variable to the Egg
/// Simplified the WriteStep function to automatically turn any step if written in camel into a proper string
///
/// V3.0.0
/// Added length check for the client and server token
/// Added a token prefix check and a warning if the token doesn't match the prefix format (only warning due to the prefix having changed before and shouldn't break anything if they do again)
/// Switched the loading order of secrets and config to be config then secrets
/// Removed the MessageHistory JSON due to it being redundant with message lookup
/// Removed Message History Testing, and any file loading tests or references
/// Added Default Debug being toggled on if in debug built in Editor
/// Fixed the console output to display Debug outputs as well now
/// Fixed Output mode to work as intended now when set
/// Fixed Inverted dry run behavior in consolidated mode
/// Removed message history cache validation due to redundancy
/// Added Last Message Checks when getting the last cache entry and nothing is found in the cache
/// Added setup instructions for the Pelican Egg into the README.md
/// Added Maximum CPU, Memory, and Disk usage to the Markdown as variables that can be used
/// Added Embed size checks before sending the message, and stopping the message from being sent if the embed goes beyond discord specifications
/// Added BadRequestException (aka. Error code 400) to be caught to better catch if discord refuses the message (mostly due to its size)
/// Fixed Start and Stop server discord interaction to not use the right ID which caused it to fail
/// Fixed Start and Stop server discord interaction response to stop throwing errors and actually responding to the interaction
/// Switched IsBot check behind the ID check to stop unnecessary checks
/// Fixed IgnoreInternalServers removing ever server due to it checking before allocations get populated.
/// Changed The ExtractRconPort and ExtractQueryPort function to use the already existing list of network allocations for that server instead of extracting it again
/// Fixed the Discord Interactions to be more robust and not giving a discord response in paginated
/// Reduced the amount of duplicate code in button creation
/// Removed a lot of boiler plate and duplicate code in Paginated Message creation and updating
/// Fixed Buttons not being updated in Paginated Format if message previously existed
/// Fixed Page Flipping discord interaction
/// Fixed Start and Stop buttons not being added or removed if start and stop permission changed
/// Changed all the start stop button debug output to use the debug output tag
/// Removed Duplicate code during button creation
/// Added more Debug logging to the JSON information extraction
/// Replaced all references to Debug checks before sending a debug output with the debug tag
/// Added more error handling when flipping pages, to handle more edge cases
/// Added recursive search as a fallback to the JSON Element extraction
/// Cleaned up the code for getting the server list and all its information together to transform it into more of a 1 function 1 purpose
///
/// V3.0.1
/// Added a special Update script for Pelican and Docker container that runs before the bot and not after the bot is started
/// Added a disable environment variable for the updater class if inside container
/// 
/// 
/// </summary>

//TODO: Check if long term usage increases RAM usage over 100mb (too many collections and list that never got caught by GC would cause this)
public static class Program
{
    private static List<DiscordChannel> _targetChannel = null!;
    public static Secrets Secrets = null!;
    public static Config Config = null!;
    internal static readonly EmbedBuilderService EmbedService = new();
    internal static List<DiscordEmbed> EmbedPages = null!;
    internal static List<ServerInfo> GlobalServerInfo = null!;
    internal static ulong BotId;

    private static async Task Main()
    {
        _targetChannel = [];
        
        await FileManager.ReadConfigFile();
        await FileManager.ReadSecretsFile();
        
        #if DEBUG
            Config.MessageFormat = MessageFormat.Consolidated;
            Config.Debug = true;
            Config.LimitServerCount = true;
            Config.MaxServerCount = 20;
            Config.IgnoreInternalServers = true;
            Config.ServersToIgnore = ["c76c19e3-85f4-41b1-9cd4-0699dfcc78e9"];
        #endif

        if (FileManager.GetFilePath("MessageMarkdown.txt") == string.Empty)
        {
            Console.WriteLine("MessageMarkdown.txt not found. Pulling Default from Github!");
            _ = FileManager.CreateMessageMarkdownFile();
        }

        GetGamesToMonitorFileAsync();
        ServerMarkdown.GetMarkdownFileContentAsync();
        
        WriteLine($"The Bot is currently on version {VersionUpdater.CurrentVersion}");

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
        BotId = discord.CurrentUser.Id;
        await Task.Delay(-1);
    }
    
    /// <summary>
    /// Function that is called when the bot is ready to send messages.
    /// </summary>
    /// <param name="sender">DiscordClient</param>
    /// <param name="e">ReadyEventArgs</param>
    private static async Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        WriteLine("Bot is connected and ready!");
        if (Secrets.ChannelIds != null)
        {
            foreach (var targetChannel in Secrets.ChannelIds)
            {
                var discordChannel = await sender.GetChannelAsync(targetChannel);
                _targetChannel.Add(discordChannel);
                WriteLine($"Target channel: {discordChannel.Name}");
            }

            _ = StartStatsUpdater(sender, Secrets.ChannelIds);
        }
        else
        {
            WriteLine("ChannelIds in the Secrets File is empty or not spelled correctly!", CurrentStep.None, OutputType.Error);
        }
    }

    /// <summary>
    /// Starts the Sever statistics updater loop.
    /// </summary>
    /// <param name="client">Bot client</param>
    /// <param name="channelIds">Target channels</param>
    private static Task StartStatsUpdater(DiscordClient client, ulong[] channelIds)
    {
        switch (Config.MessageFormat)
        {
            // When the config is set to consolidate embeds
            case MessageFormat.Consolidated:
                Consolidated.ConsolidatedUpdateLoop(client, channelIds);
                break;
            // When the config is set to paginate
            case MessageFormat.Paginated:
                Paginated.PaginatedUpdateLoop(client, channelIds);
                break;
            // When the config is set to PerServerMessages
            case MessageFormat.PerServer:
                PerServer.PerServerUpdateLoop(client, channelIds);
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the embed updater loop called by the StartStatsUpdater method
    /// </summary>
    /// <param name="mode">The EmbedUpdateMode to use</param>
    /// <param name="generateEmbedsAsync">A function that generates the embeds</param>
    /// <param name="applyEmbedUpdateAsync">A function that applies the embed update</param>
    /// <param name="delaySeconds">Delay in seconds between updates</param>
    public static void StartEmbedUpdaterLoop(MessageFormat mode, Func<Task<(List<string?> uuids, object embedOrEmbeds)>> generateEmbedsAsync, Func<object, List<string?>, Task> applyEmbedUpdateAsync, int delaySeconds = 10)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    var (uuids, embedData) = await generateEmbedsAsync();
                    await applyEmbedUpdateAsync(embedData, uuids);
                    WriteLine($"Updated Embed Data at {DateTime.Now}");
                }
                catch (BadRequestException ex)
                {
                    WriteLine("Bad request when sending message, which is usually triggered by a message that is too long.", CurrentStep.DiscordMessage, OutputType.Error, ex);
                }
                catch (Exception ex)
                {
                    WriteLine($"Updater error for mode {mode}", CurrentStep.None, OutputType.Error, ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        });
    }
}