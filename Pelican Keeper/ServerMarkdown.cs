using System.Text.RegularExpressions;

namespace Pelican_Keeper;

public static class ServerMarkdown
{
    private record PreprocessedTemplate(string Body, Dictionary<string, string> Tags);
    private static readonly string MessageMarkdownPath = FileManager.GetFilePath("MessageMarkdown.txt");
    private static string _templateText = File.ReadAllText(MessageMarkdownPath);

    private static PreprocessedTemplate PreprocessTemplateTags(object model)
    {
        var tagDict = new Dictionary<string, string>();
        var tagRegex = new Regex(@"\[(\w+)](.*?)\[/\1]", RegexOptions.Singleline);
        string strippedTemplate = tagRegex.Replace(_templateText, match =>
        {
            string tag = match.Groups[1].Value;
            string content = match.Groups[2].Value;
            string rendered = ReplacePlaceholders(content, model);
            tagDict[tag] = rendered.Trim();
            return "";
        });
        return new PreprocessedTemplate(strippedTemplate.Trim(), tagDict);
    }

    private static string ReplacePlaceholders(string text, object model)
    {
        return Regex.Replace(text, @"\{\{(\w+)\}\}", match =>
        {
            var propName = match.Groups[1].Value;
            var prop = model.GetType().GetProperty(propName);
            if (prop == null) return match.Value;
            var value = prop.GetValue(model);
            return value?.ToString() ?? "";
        });
    }

    public static (string message, string serverName) ParseTemplate(TemplateClasses.ServerInfo serverResponse)
    {
        if (serverResponse.Resources == null) throw new ArgumentException("Server Resource response cannot be null.");

        // Calculate Memory String
        string memoryString = EmbedBuilderHelper.FormatBytes(serverResponse.Resources.MemoryBytes);
        if (serverResponse.MaxMemoryBytes > 0)
        {
            memoryString += " / " + EmbedBuilderHelper.FormatBytes(serverResponse.MaxMemoryBytes);
        }

        var viewModel = new TemplateClasses.ServerViewModel
        {
            Uuid = serverResponse.Uuid,
            ServerName = serverResponse.Name,
            Status = serverResponse.Resources.CurrentState,
            StatusIcon = EmbedBuilderHelper.GetStatusIcon(serverResponse.Resources.CurrentState),
            Cpu = $"{serverResponse.Resources.CpuAbsolute:0.00}%",
            Memory = memoryString,
            Disk = EmbedBuilderHelper.FormatBytes(serverResponse.Resources.DiskBytes),
            NetworkRx = EmbedBuilderHelper.FormatBytes(serverResponse.Resources.NetworkRxBytes),
            NetworkTx = EmbedBuilderHelper.FormatBytes(serverResponse.Resources.NetworkTxBytes),
            Uptime = EmbedBuilderHelper.FormatUptime(serverResponse.Resources.Uptime)
        };

        if (Program.Config.JoinableIpDisplay)
        {
            viewModel.IpAndPort = HelperClass.GetReadableConnectableAddress(serverResponse);
        }

        if (Program.Config.PlayerCountDisplay)
        {
            viewModel.PlayerCount = string.IsNullOrEmpty(serverResponse.PlayerCountText) ? "N/A" : serverResponse.PlayerCountText;
        }

        var result = PreprocessTemplateTags(viewModel);
        var serverName = result.Tags.GetValueOrDefault("Title", "Default Title");
        var message = ReplacePlaceholders(result.Body, viewModel);

        if (Program.Config.Debug)
            ConsoleExt.WriteLineWithStepPretext($"Server: {viewModel.ServerName}, Message Character Count: {message.Length}", ConsoleExt.CurrentStep.Markdown);

        return (message, serverName);
    }

    public static void GetMarkdownFileContentAsync()
    {
        Task.Run(async () =>
        {
            while (Program.Config.ContinuesMarkdownRead)
            {
                _templateText = await File.ReadAllTextAsync(MessageMarkdownPath);
                await Task.Delay(TimeSpan.FromSeconds(Program.Config.MarkdownUpdateInterval));
            }
        });
    }
}