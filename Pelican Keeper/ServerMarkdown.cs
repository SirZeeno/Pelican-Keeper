using System.Globalization;
using System.Text.RegularExpressions;
using Pelican_Keeper.Helper_Classes;

namespace Pelican_Keeper;

using static EmbedBuilderHelper;
using static ConversionHelpers;


public static class ServerMarkdown
{
    private record PreprocessedTemplate(string Body, Dictionary<string, string> Tags);
    private static readonly string MessageMarkdownPath = FileManager.GetFilePath("MessageMarkdown.txt"); //TODO: Add error handling for missing file and Validation for correct format
    private static string _templateText = File.ReadAllText(MessageMarkdownPath);

    /// <summary>
    /// Processes [Tag]...[/Tag] blocks, replaces placeholders inside them,
    /// and removes them from the final template body.
    /// </summary>
    /// <param name="model">The template model to use</param>
    /// <returns>A preprocessed template</returns>
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
            return ""; // removes the tagged content from the final template body
        });

        return new PreprocessedTemplate(strippedTemplate.Trim(), tagDict);
    }

    /// <summary>
    /// Replaces {{VariableName}} placeholders in text using reflection on the model.
    /// </summary>
    /// <param name="text">Input text</param>
    /// <param name="model">The template model to use</param>
    /// <returns>The text with placeholders replaced</returns>
    private static string ReplacePlaceholders(string text, object model)
    {
        return Regex.Replace(text, @"\{\{(\w+)\}\}", match =>
        {
            var propName = match.Groups[1].Value;
            var prop = model.GetType().GetProperty(propName);
            if (prop == null) return match.Value; // leaves the placeholder as-is if not found
            var value = prop.GetValue(model);
            return value?.ToString() ?? "";
        });
    }

    /// <summary>
    /// Parses the message template and returns the final message and server title.
    /// </summary>
    /// <param name="serverResponse">The server response</param>
    /// <returns>A tuple containing the final message and server title</returns>
    public static (string message, string serverName) ParseTemplate(TemplateClasses.ServerInfo serverResponse)
    {
        if (serverResponse.Resources == null) throw new ArgumentException("Server Resource response cannot be null.");
        
        var viewModel = new TemplateClasses.ServerViewModel
        {
            Uuid = serverResponse.Uuid,
            ServerName = serverResponse.Name,
            Status = serverResponse.Resources.CurrentState,
            StatusIcon = GetStatusIcon(serverResponse.Resources.CurrentState),
            Cpu = $"{serverResponse.Resources.CpuAbsolute:0.00}%",
            MaxCpu = $"{DynamicallyAddPercentSign(IfZeroThenInfinite(serverResponse.Resources.CpuMaximum.ToString(CultureInfo.InvariantCulture)))}",
            Memory = FormatBytes(serverResponse.Resources.MemoryBytes),
            MaxMemory = IfZeroThenInfinite(serverResponse.Resources.MemoryMaximum.ToString(CultureInfo.InvariantCulture)),
            Disk = FormatBytes(serverResponse.Resources.DiskBytes),
            MaxDisk = IfZeroThenInfinite(serverResponse.Resources.DiskMaximum.ToString(CultureInfo.InvariantCulture)),
            NetworkRx = FormatBytes(serverResponse.Resources.NetworkRxBytes),
            NetworkTx = FormatBytes(serverResponse.Resources.NetworkTxBytes),
            Uptime = FormatUptime(serverResponse.Resources.Uptime)
        };

        if (Program.Config.JoinableIpDisplay) viewModel.IpAndPort = ExtractorHelpers.GetReadableConnectableAddress(serverResponse);
        
        if (Program.Config.PlayerCountDisplay) viewModel.PlayerCount = string.IsNullOrEmpty(serverResponse.PlayerCountText) ? "N/A" : serverResponse.PlayerCountText;

        var result = PreprocessTemplateTags(viewModel);
        var serverName = result.Tags.GetValueOrDefault("Title", "Default Title");
        var message = ReplacePlaceholders(result.Body, viewModel);

        ConsoleExt.WriteLine($"Server: {viewModel.ServerName}, Message Character Count: {message.Length}", ConsoleExt.CurrentStep.Markdown, ConsoleExt.OutputType.Debug);

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