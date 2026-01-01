using DSharpPlus.Entities;
using Pelican_Keeper.Core;
using Pelican_Keeper.Models;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Discord;

/// <summary>
/// Builds Discord embeds for server status display.
/// </summary>
public class EmbedBuilderService
{
    /// <summary>
    /// Builds a single-server embed for PerServer display mode.
    /// </summary>
    public Task<DiscordEmbed> BuildSingleServerEmbedAsync(ServerInfo server)
    {
        var (message, serverName) = ServerMarkdownParser.ParseTemplate(server);

        var embed = new DiscordEmbedBuilder
        {
            Title = serverName,
            Color = DiscordColor.Azure
        };

        embed.AddField("\u200B", message, inline: true);

        if (RuntimeContext.Config.DryRun)
        {
            Logger.WriteLineWithStep(serverName, Logger.Step.EmbedBuilding);
            Logger.WriteLineWithStep(message, Logger.Step.EmbedBuilding);
        }

        embed.Footer = new DiscordEmbedBuilder.EmbedFooter
        {
            Text = $"Last Updated: {DateTime.Now:HH:mm:ss}"
        };

        if (RuntimeContext.Config.Debug)
        {
            Logger.WriteLineWithStep($"Embed character count: {GetEmbedCharacterCount(embed)}", Logger.Step.EmbedBuilding);
        }

        return Task.FromResult(embed.Build());
    }

    /// <summary>
    /// Builds a multi-server embed for Consolidated display mode.
    /// </summary>
    public Task<DiscordEmbed> BuildMultiServerEmbedAsync(List<ServerInfo> servers)
    {
        var embed = new DiscordEmbedBuilder
        {
            Title = "📡 Game Server Status Overview",
            Color = DiscordColor.Azure
        };

        for (int i = 0; i < servers.Count && embed.Fields.Count < 25; i++)
        {
            var (message, serverName) = ServerMarkdownParser.ParseTemplate(servers[i]);
            embed.AddField(serverName, message, inline: true);

            if (RuntimeContext.Config.DryRun)
            {
                Logger.WriteLineWithStep(serverName, Logger.Step.EmbedBuilding);
                Logger.WriteLineWithStep(message, Logger.Step.EmbedBuilding);
            }
        }

        embed.Footer = new DiscordEmbedBuilder.EmbedFooter
        {
            Text = $"Last Updated: {DateTime.Now:HH:mm:ss}"
        };

        if (RuntimeContext.Config.Debug)
        {
            Logger.WriteLineWithStep($"Embed character count: {GetEmbedCharacterCount(embed)}", Logger.Step.EmbedBuilding);
        }

        return Task.FromResult(embed.Build());
    }

    /// <summary>
    /// Builds paginated embeds for Paginated display mode.
    /// </summary>
    public Task<List<DiscordEmbed>> BuildPaginatedEmbedsAsync(List<ServerInfo> servers)
    {
        var embeds = new List<DiscordEmbed>();

        foreach (var server in servers)
        {
            var (message, serverName) = ServerMarkdownParser.ParseTemplate(server);

            var embed = new DiscordEmbedBuilder
            {
                Title = serverName,
                Color = DiscordColor.Azure
            };

            embed.AddField("\u200B", message, true);

            if (RuntimeContext.Config.DryRun)
            {
                Logger.WriteLineWithStep(serverName, Logger.Step.EmbedBuilding);
                Logger.WriteLineWithStep(message, Logger.Step.EmbedBuilding);
            }

            embed.Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = $"Last Updated: {DateTime.Now:HH:mm:ss}"
            };

            if (RuntimeContext.Config.Debug)
            {
                Logger.WriteLineWithStep($"Embed character count: {GetEmbedCharacterCount(embed)}", Logger.Step.EmbedBuilding);
            }

            embeds.Add(embed.Build());
        }

        return Task.FromResult(embeds);
    }

    private static int GetEmbedCharacterCount(DiscordEmbedBuilder embed)
    {
        var count = 0;
        if (embed.Title != null) count += embed.Title.Length;
        if (embed.Description != null) count += embed.Description.Length;
        if (embed.Footer?.Text != null) count += embed.Footer.Text.Length;
        if (embed.Author?.Name != null) count += embed.Author.Name.Length;
        foreach (var field in embed.Fields)
        {
            count += field.Name?.Length ?? 0;
            count += field.Value?.Length ?? 0;
        }
        return count;
    }
}
