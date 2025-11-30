using DSharpPlus.Entities;

namespace Pelican_Keeper;
using static TemplateClasses;

public class EmbedBuilderService
{
    public Task<DiscordEmbed> BuildSingleServerEmbed(ServerInfo server)
    {
        var serverInfo = ServerMarkdown.ParseTemplate(server);
        
        var embed = new DiscordEmbedBuilder
        {
            Title = serverInfo.serverName,
            Color = DiscordColor.Azure
        };
        
        embed.AddField("\u200B", serverInfo.message, inline: true);
        
        if (Program.Config.DryRun)
        {
            ConsoleExt.WriteLineWithStepPretext(serverInfo.serverName, ConsoleExt.CurrentStep.EmbedBuilding);
            ConsoleExt.WriteLineWithStepPretext(serverInfo.message, ConsoleExt.CurrentStep.EmbedBuilding);
        }
        
        embed.Footer = new DiscordEmbedBuilder.EmbedFooter
        {
            Text = $"Last Updated: {DateTime.Now:HH:mm:ss}" //TODO: allow for an expanded format per users choice like this DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")
        };
        
        if (!Program.Config.Debug) return Task.FromResult(embed.Build());
        
        ConsoleExt.WriteLineWithStepPretext("Last Updated: " + DateTime.Now.ToString("HH:mm:ss"), ConsoleExt.CurrentStep.EmbedBuilding);
        ConsoleExt.WriteLineWithStepPretext($"Embed character count: {EmbedBuilderHelper.GetEmbedCharacterCount(embed)}", ConsoleExt.CurrentStep.EmbedBuilding);
        return Task.FromResult(embed.Build());
    }

    public Task<DiscordEmbed> BuildMultiServerEmbed(List<ServerInfo> servers) //TODO: Add the ability to use the game icon as the emoji next to the server name
    {
        var embed = new DiscordEmbedBuilder
        {
            Title = "📡 Game Server Status Overview",
            Color = DiscordColor.Azure
        };

        for (int i = 0; i < servers.Count && embed.Fields.Count < 25; i++)
        {
            var serverInfo = ServerMarkdown.ParseTemplate(servers[i]);
            embed.AddField(serverInfo.serverName, serverInfo.message, inline: true); //TODO:Allow customization of this but test first if this is worth customizing
            
            if (Program.Config.DryRun)
            {
                ConsoleExt.WriteLineWithStepPretext(serverInfo.serverName, ConsoleExt.CurrentStep.EmbedBuilding);
                ConsoleExt.WriteLineWithStepPretext(serverInfo.message, ConsoleExt.CurrentStep.EmbedBuilding);
            }
        }
        
        embed.Footer = new DiscordEmbedBuilder.EmbedFooter
        {
            Text = $"Last Updated: {DateTime.Now:HH:mm:ss}"
        };
        
        if (!Program.Config.Debug) return Task.FromResult(embed.Build());
        
        ConsoleExt.WriteLineWithStepPretext("Last Updated: " + DateTime.Now.ToString("HH:mm:ss"), ConsoleExt.CurrentStep.EmbedBuilding);
        ConsoleExt.WriteLineWithStepPretext($"Embed character count: {EmbedBuilderHelper.GetEmbedCharacterCount(embed)}", ConsoleExt.CurrentStep.EmbedBuilding);
        return Task.FromResult(embed.Build());
    }

    public Task<List<DiscordEmbed>> BuildPaginatedServerEmbeds(List<ServerInfo> servers)
    {
        var embeds = new List<DiscordEmbed>();

        for (int i = 0; i < servers.Count; i++)
        {
            var server = servers[i];

            var serverInfo = ServerMarkdown.ParseTemplate(server);

            var embed = new DiscordEmbedBuilder
            {
                Title = serverInfo.serverName,
                Color = DiscordColor.Azure
            };

            embed.AddField("\u200B", serverInfo.message,true);
            
            if (Program.Config.DryRun)
            {
                ConsoleExt.WriteLineWithStepPretext(serverInfo.serverName, ConsoleExt.CurrentStep.EmbedBuilding);
                ConsoleExt.WriteLineWithStepPretext(serverInfo.message, ConsoleExt.CurrentStep.EmbedBuilding);
            }
            
            embed.Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = $"Last Updated: {DateTime.Now:HH:mm:ss}"
            };
            if (Program.Config.Debug)
            {
                ConsoleExt.WriteLineWithStepPretext("Last Updated: " + DateTime.Now.ToString("HH:mm:ss"), ConsoleExt.CurrentStep.EmbedBuilding);
                ConsoleExt.WriteLineWithStepPretext($"Embed character count: {EmbedBuilderHelper.GetEmbedCharacterCount(embed)}", ConsoleExt.CurrentStep.EmbedBuilding);
            }
            embeds.Add(embed.Build());
        }
        
        return Task.FromResult(embeds);
    }
}

public static class EmbedBuilderHelper
{
    // Keeping for future reference, if needed. Currently not used or planned to be used.
    public static void SafeAddField(this DiscordEmbedBuilder builder, string name, string? value, bool inline = false)
    {
        builder.AddField(name, string.IsNullOrEmpty(value) ? "N/A" : value, inline);
    }

    internal static string FormatBytes(long bytes)
    {
        const long kb = 1024;
        const long mb = kb * 1024;
        const long gb = mb * 1024;
        const long tb = gb * 1024;

        return bytes switch
        {
            >= tb => $"{bytes / (double)tb:F2} TiB",
            >= gb => $"{bytes / (double)gb:F2} GiB",
            >= mb => $"{bytes / (double)mb:F2} MiB",
            >= kb => $"{bytes / (double)kb:F2} KiB",
            _ => $"{bytes} B"
        };
    }

    internal static string FormatUptime(long uptimeMs)
    {
        var uptime = TimeSpan.FromMilliseconds(uptimeMs);
        return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
    }

    public static string GetStatusIcon(string status)
    {
        return status.ToLower() switch
        {
            "offline" => "🔴",
            "missing" => "🟡",
            "running" => "🟢",
            _ => "⚪"
        };
    }
    
    internal static int GetEmbedCharacterCount(DiscordEmbedBuilder embed)
    {
        var count = 0;

        if (embed.Title != null)
            count += embed.Title.Length;

        if (embed.Description != null)
            count += embed.Description.Length;

        if (embed.Footer?.Text != null)
            count += embed.Footer.Text.Length;

        if (embed.Author?.Name != null)
            count += embed.Author.Name.Length;

        foreach (var field in embed.Fields)
        {
            count += field.Name?.Length ?? 0;
            count += field.Value?.Length ?? 0;
        }

        return count;
    }
}

