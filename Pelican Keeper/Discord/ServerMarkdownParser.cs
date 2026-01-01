using System.Text.RegularExpressions;
using Pelican_Keeper.Core;
using Pelican_Keeper.Models;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Discord;

/// <summary>
/// Parses MessageMarkdown.txt templates and replaces placeholders with server data.
/// </summary>
public static class ServerMarkdownParser
{
    private static string _markdownPath = "";
    private static string _templateText = "";

    /// <summary>
    /// Loads the template file asynchronously.
    /// </summary>
    public static async Task LoadTemplateAsync()
    {
        _markdownPath = Configuration.FileManager.GetFilePath("MessageMarkdown.txt");
        if (File.Exists(_markdownPath))
            _templateText = await File.ReadAllTextAsync(_markdownPath);
    }

    /// <summary>
    /// Parses the template with server data and returns formatted message and title.
    /// </summary>
    public static (string message, string serverName) ParseTemplate(ServerInfo server)
    {
        if (server.Resources == null)
            throw new ArgumentException("Server resources cannot be null.");

        var memoryString = FormatHelper.FormatBytes(server.Resources.MemoryBytes);
        if (server.MaxMemoryBytes > 0)
            memoryString += " / " + FormatHelper.FormatBytes(server.MaxMemoryBytes);

        var viewModel = new ServerViewModel
        {
            Uuid = server.Uuid,
            ServerName = server.Name,
            Status = server.Resources.CurrentState,
            StatusIcon = FormatHelper.GetStatusIcon(server.Resources.CurrentState),
            Cpu = $"{server.Resources.CpuAbsolute:0.00}%",
            Memory = memoryString,
            Disk = FormatHelper.FormatBytes(server.Resources.DiskBytes),
            NetworkRx = FormatHelper.FormatBytes(server.Resources.NetworkRxBytes),
            NetworkTx = FormatHelper.FormatBytes(server.Resources.NetworkTxBytes),
            Uptime = FormatHelper.FormatUptime(server.Resources.Uptime)
        };

        if (RuntimeContext.Config.JoinableIpDisplay)
            viewModel.IpAndPort = NetworkHelper.GetConnectAddress(server);

        if (RuntimeContext.Config.PlayerCountDisplay)
            viewModel.PlayerCount = string.IsNullOrEmpty(server.PlayerCountText) ? "N/A" : server.PlayerCountText;

        var (body, tags) = PreprocessTemplateTags(viewModel);
        var serverName = tags.GetValueOrDefault("Title", "Default Title");
        var message = ReplacePlaceholders(body, viewModel);

        if (RuntimeContext.Config.Debug)
            Logger.WriteLineWithStep($"Server: {viewModel.ServerName}, Message Length: {message.Length}", Logger.Step.Markdown);

        return (message, serverName);
    }

    /// <summary>
    /// Starts background task for continuous template reloading.
    /// </summary>
    public static void StartContinuousReload()
    {
        Task.Run(async () =>
        {
            while (RuntimeContext.Config.ContinuesMarkdownRead)
            {
                if (File.Exists(_markdownPath))
                    _templateText = await File.ReadAllTextAsync(_markdownPath);

                await Task.Delay(TimeSpan.FromSeconds(RuntimeContext.Config.MarkdownUpdateInterval));
            }
        });
    }

    private static (string body, Dictionary<string, string> tags) PreprocessTemplateTags(object model)
    {
        var tagDict = new Dictionary<string, string>();
        var tagRegex = new Regex(@"\[(\w+)](.*?)\[/\1]", RegexOptions.Singleline);

        var strippedTemplate = tagRegex.Replace(_templateText, match =>
        {
            var tag = match.Groups[1].Value;
            var content = match.Groups[2].Value;
            var rendered = ReplacePlaceholders(content, model);
            tagDict[tag] = rendered.Trim();
            return "";
        });

        return (strippedTemplate.Trim(), tagDict);
    }

    private static string ReplacePlaceholders(string text, object model)
    {
        return Regex.Replace(text, @"\{\{(\w+)\}\}", match =>
        {
            var propName = match.Groups[1].Value;
            var prop = model.GetType().GetProperty(propName);
            return prop?.GetValue(model)?.ToString() ?? match.Value;
        });
    }
}
