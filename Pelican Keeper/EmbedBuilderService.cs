using DSharpPlus.Entities;

namespace Pelican_Keeper;

using static TemplateClasses;

public class
    EmbedBuilderService //TODO: allow of sending multiple messages if the current message is too long to allow fitting in more servers
{
    public Task<DiscordEmbed> BuildSingleServerEmbed(ServerInfo server)
    {
        var serverInfo = ServerMarkdown.ParseTemplate(server);

        var embed = new DiscordEmbedBuilder { Title = serverInfo.serverName, Color = DiscordColor.Azure };

        embed.AddField("\u200B", serverInfo.message, true);

        if (Program.Config.DryRun)
        {
            ConsoleExt.WriteLine(serverInfo.serverName, ConsoleExt.CurrentStep.EmbedBuilding);
            ConsoleExt.WriteLine(serverInfo.message, ConsoleExt.CurrentStep.EmbedBuilding);
        }

        if (!string.IsNullOrWhiteSpace(Program.Config.CustomDateTimeFormat) &&
            !string.IsNullOrEmpty(Program.Config.CustomDateTimeFormat))
            embed.Footer = new DiscordEmbedBuilder.EmbedFooter
                { Text = $"Last Updated: {DateTime.Now.ToString(Program.Config.CustomDateTimeFormat)}" };
        else
            embed.Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"Last Updated: {DateTime.Now:HH:mm:ss}" };

        ConsoleExt.WriteLine("Last Updated: " + DateTime.Now.ToString("HH:mm:ss"), ConsoleExt.CurrentStep.EmbedBuilding,
            ConsoleExt.OutputType.Debug);
        ConsoleExt.WriteLine($"Embed character count: {EmbedBuilderHelper.GetEmbedCharacterCount(embed)}",
            ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        return Task.FromResult(embed.Build());
    }

    public Task<DiscordEmbed>
        BuildMultiServerEmbed(
            List<ServerInfo> servers) //TODO: Add the ability to use the game icon as the emoji next to the server name
    {
        var embed = new DiscordEmbedBuilder { Title = "📡 Game Server Status Overview", Color = DiscordColor.Azure };

        for (var i = 0; i < servers.Count && embed.Fields.Count < 25; i++)
        {
            var serverInfo = ServerMarkdown.ParseTemplate(servers[i]);
            embed.AddField(serverInfo.serverName, serverInfo.message,
                true); //TODO:Allow customization of it being inline or not but test first if this is worth customizing

            if (!Program.Config.DryRun) continue;
            ConsoleExt.WriteLine(serverInfo.serverName, ConsoleExt.CurrentStep.EmbedBuilding);
            ConsoleExt.WriteLine(serverInfo.message, ConsoleExt.CurrentStep.EmbedBuilding);
        }

        embed.Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"Last Updated: {DateTime.Now:HH:mm:ss}" };

        ConsoleExt.WriteLine("Last Updated: " + DateTime.Now.ToString("HH:mm:ss"), ConsoleExt.CurrentStep.EmbedBuilding,
            ConsoleExt.OutputType.Debug);
        ConsoleExt.WriteLine($"Embed character count: {EmbedBuilderHelper.GetEmbedCharacterCount(embed)}",
            ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        return Task.FromResult(embed.Build());
    }

    public Task<List<DiscordEmbed>> BuildPaginatedServerEmbeds(List<ServerInfo> servers)
    {
        var embeds = new List<DiscordEmbed>();

        foreach (var server in servers)
        {
            var serverInfo = ServerMarkdown.ParseTemplate(server);

            var embed = new DiscordEmbedBuilder { Title = serverInfo.serverName, Color = DiscordColor.Azure };

            embed.AddField("\u200B", serverInfo.message, true);

            if (Program.Config.DryRun)
            {
                ConsoleExt.WriteLine(serverInfo.serverName, ConsoleExt.CurrentStep.EmbedBuilding);
                ConsoleExt.WriteLine(serverInfo.message, ConsoleExt.CurrentStep.EmbedBuilding);
            }

            embed.Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"Last Updated: {DateTime.Now:HH:mm:ss}" };

            ConsoleExt.WriteLine("Last Updated: " + DateTime.Now.ToString("HH:mm:ss"),
                ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
            ConsoleExt.WriteLine($"Embed character count: {EmbedBuilderHelper.GetEmbedCharacterCount(embed)}",
                ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
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

    internal static int GetEmbedCharacterCount(DiscordEmbedBuilder embed)
    {
        var count = 0;

        if (embed.Title != null)
        {
            count += embed.Title.Length;
            if (Program.Config.Debug)
                ConsoleExt.WriteLine($"Embed Title Character count: {embed.Title.Length}", ConsoleExt.CurrentStep.EmbedBuilding);
            if (embed.Title.Length > 256)
                ConsoleExt.WriteLine("Message Title exceeds Character count of 256", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Error);
        }

        if (embed.Description != null)
        {
            count += embed.Description.Length;
            if (Program.Config.Debug)
                ConsoleExt.WriteLine($"Embed Description Character count: {embed.Description.Length}", ConsoleExt.CurrentStep.EmbedBuilding);
            if (embed.Description.Length > 4096)
                ConsoleExt.WriteLine("Message Description exceeds Character count of 4096", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Error);
        }

        if (embed.Footer?.Text != null)
        {
            count += embed.Footer.Text.Length;
            if (Program.Config.Debug)
                ConsoleExt.WriteLine($"Embed Footer Character count: {embed.Footer.Text.Length}", ConsoleExt.CurrentStep.EmbedBuilding);
            if (embed.Footer.Text.Length > 2048)
                ConsoleExt.WriteLine("Message Footer exceeds Character count of 2048", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Error);
        }

        if (embed.Author?.Name != null)
        {
            count += embed.Author.Name.Length;
            if (Program.Config.Debug)
                ConsoleExt.WriteLine($"Embed Author Character count: {embed.Author.Name.Length}", ConsoleExt.CurrentStep.EmbedBuilding);
            if (embed.Author.Name.Length > 256)
            {
                ConsoleExt.WriteLine("Message Author exceeds Character count of 256", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Error);
            }
        }

        foreach (var field in embed.Fields)
        {
            if (field.Name != null)
            {
                count += field.Name.Length;
                if (Program.Config.Debug)
                    ConsoleExt.WriteLine($"Embed {field.Name} Name Character count: {field.Name.Length}", ConsoleExt.CurrentStep.EmbedBuilding);
                if (field.Name.Length > 256)
                    ConsoleExt.WriteLine($"Message {field.Name} Field Name exceeds Character count of 256", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Error);
            }

            if (field.Value != null)
            {
                count += field.Value.Length;
                if (Program.Config.Debug)
                    ConsoleExt.WriteLine($"Embed {field.Name} Value Character count: {field.Value.Length}", ConsoleExt.CurrentStep.EmbedBuilding);
                if (field.Value.Length > 1024)
                    ConsoleExt.WriteLine($"Message {field.Name} Field Value exceeds Character count of 1024", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Error);
            }
        }

        if (count > 6000)
            ConsoleExt.WriteLine("Message total exceeds Character count of 6000", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Error);
        return count;
    }
}