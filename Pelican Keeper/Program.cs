using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Pelican_Keeper;

using Update_Loops;

using static TemplateClasses;
using static PelicanInterface;
using static ConsoleExt;
using static DiscordInteractions;

/// <summary>
/// Changelog
/// 
/// Added Previous Message checks to the cache validation to try and get the previous message in the target channel if none of the messages in the cache exist.
/// Fixed Redundant double checks to the message history cache
/// Split each message update loop into its own class
/// Added the Version parameter into the project config and into the Update checker.
/// Added OutputMode into the config to further narrow down what debug levels should be written into console
/// Added an option that allows the bypass of the debug config check for console outputs
/// Changed the Default for Debug mode to false
/// Added the OutputMode variable to the Egg
/// 
/// </summary>


public static class Program
{
    internal static List<DiscordChannel> TargetChannel = null!;
    public static Secrets Secrets = null!;
    public static Config Config = null!;
    internal static readonly EmbedBuilderService EmbedService = new();
    internal static List<DiscordEmbed> EmbedPages = null!;
    internal static List<ServerInfo> GlobalServerInfo = null!;
    internal static ulong BotId;


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
        BotId = discord.CurrentUser.Id;
        
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
        WriteLine("Bot is connected and ready!");
        if (Secrets.ChannelIds != null)
        {
            foreach (var targetChannel in Secrets.ChannelIds)
            {
                var discordChannel = await sender.GetChannelAsync(targetChannel);
                TargetChannel.Add(discordChannel);
                WriteLine($"Target channel: {discordChannel.Name}");
            }

            _ = StartStatsUpdater(sender, Secrets.ChannelIds);
        }
        else
        {
            WriteLine("ChannelIds in the Secrets File is empty or not spelled correctly!", CurrentStep.Ignore, OutputType.Error);
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
                }
                catch (Exception ex)
                {
                    WriteLine($"Updater error for mode {mode}: {ex.Message}", CurrentStep.Ignore, OutputType.Warning);
                    WriteLine($"Stack trace: {ex.StackTrace}", CurrentStep.Ignore, OutputType.Warning);
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
    public static async Task<DiscordMessage> SendPaginatedMessageAsync(this DiscordChannel channel, List<DiscordEmbed> embeds, string? serverUuidStart = null, string? serverUuidStop = null)
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