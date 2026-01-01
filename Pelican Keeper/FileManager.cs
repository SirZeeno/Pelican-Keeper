using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Pelican_Keeper;

using static ConsoleExt;
using static TemplateClasses;

public static class FileManager
{
    public static string GetFilePath(string fileNameWithExtension, string? originDirectory = null)
    {
        if (File.Exists(fileNameWithExtension)) return fileNameWithExtension;
        if (string.IsNullOrEmpty(originDirectory)) originDirectory = Environment.CurrentDirectory;
        foreach (var file in Directory.GetFiles(originDirectory, fileNameWithExtension, SearchOption.AllDirectories)) return file;
        WriteLineWithStepPretext($"Couldn't find {fileNameWithExtension} file in program directory!", CurrentStep.FileReading, OutputType.Error, new FileNotFoundException());
        return string.Empty;
    }

    public static string GetCustomFilePath(string fileNameWithExtension, string? customDirectoryOrFile = null)
    {
        if (string.IsNullOrEmpty(customDirectoryOrFile))
            return File.Exists(customDirectoryOrFile) ? customDirectoryOrFile : GetFilePath(fileNameWithExtension, customDirectoryOrFile);
        return GetFilePath(fileNameWithExtension);
    }

    private static async Task CreateSecretsFile()
    {
        WriteLineWithStepPretext("Secrets.json not found. Creating default one.", CurrentStep.FileReading, OutputType.Warning);
        await using var secretsFile = File.Create("Secrets.json");
        string defaultSecrets = "{\n  \"ClientToken\": \"YOUR_CLIENT_TOKEN\",\n  \"ServerToken\": \"YOUR_SERVER_TOKEN\",\n  \"ServerUrl\": \"YOUR_BASIC_SERVER_URL\",\n  \"BotToken\": \"YOUR_DISCORD_BOT_TOKEN\",\n  \"ChannelIds\": [THE_CHANNEL_ID_YOU_WANT_THE_BOT_TO_POST_IN],\n  \"ExternalServerIp\": \"YOUR_EXTERNAL_SERVER_IP\"\n}";
        await using var writer = new StreamWriter(secretsFile);
        await writer.WriteAsync(defaultSecrets);
        WriteLineWithStepPretext("Created default Secrets.json. Please fill out the values.", CurrentStep.FileReading, OutputType.Warning);
    }

    private static async Task CreateConfigFile()
    {
        await using var configFile = File.Create("Config.json");
        var defaultConfig = await HelperClass.GetJsonTextAsync("https://raw.githubusercontent.com/SirZeeno/Pelican-Keeper/refs/heads/testing/Pelican%20Keeper/Config.json");
        await using var writer = new StreamWriter(configFile);
        await writer.WriteAsync(defaultConfig);
    }

    private static async Task CreateGamesToMonitorFile()
    {
        await using var gamesToMonitorFile = File.Create("GamesToMonitor.json");
        var gamesToMonitor = await HelperClass.GetJsonTextAsync("https://raw.githubusercontent.com/SirZeeno/Pelican-Keeper/refs/heads/testing/Pelican%20Keeper/GamesToMonitor.json");
        await using var writer = new StreamWriter(gamesToMonitorFile);
        await writer.WriteAsync(gamesToMonitor);
    }

    public static async Task CreateMessageMarkdownFile()
    {
        await using var messageMarkdownFile = File.Create("MessageMarkdown.txt");
        var defaultMarkdown = await HelperClass.GetJsonTextAsync("https://raw.githubusercontent.com/SirZeeno/Pelican-Keeper/refs/heads/testing/Pelican%20Keeper/MessageMarkdown.txt");
        await using var writer = new StreamWriter(messageMarkdownFile);
        await writer.WriteAsync(defaultMarkdown);
    }

    public static async Task<Secrets?> ReadSecretsFile(string? customDirectoryOrFile = null)
    {
        string secretsPath;
        if (string.IsNullOrEmpty(customDirectoryOrFile)) secretsPath = File.Exists(customDirectoryOrFile) ? customDirectoryOrFile : GetFilePath("Secrets.json", customDirectoryOrFile);
        else secretsPath = GetFilePath("Secrets.json");

        if (secretsPath == string.Empty)
        {
            WriteLineWithStepPretext("Secrets.json not found. Creating default one.", CurrentStep.FileReading, OutputType.Warning);
            if (string.IsNullOrEmpty(customDirectoryOrFile)) { await CreateSecretsFile(); secretsPath = GetFilePath("Secrets.json"); }
            else { WriteLineWithStepPretext("Custom File or Directory specified, but unable to find Secrets File there!", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true); return null; }
            if (secretsPath == string.Empty) { WriteLineWithStepPretext("Unable to Find Secrets.json!", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true); return null; }
        }

        try
        {
            var settings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore, NullValueHandling = NullValueHandling.Include, Error = (sender, args) => { args.ErrorContext.Handled = true; } };
            var secretsJson = await File.ReadAllTextAsync(secretsPath);
            var secrets = JsonConvert.DeserializeObject<Secrets>(secretsJson, settings);
            Validator.ValidateSecrets(secrets);
            if (secrets == null) { WriteLineWithStepPretext("Secrets file is empty or not in the correct format.", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true); return null; }
            Program.Secrets = secrets;
            return secrets;
        }
        catch (Exception ex) { WriteLineWithStepPretext("Failed to load secrets.", CurrentStep.FileReading, OutputType.Error, ex, true); return null; }
    }

    public static async Task<Config?> ReadConfigFile(string? customDirectoryOrFile = null)
    {
        string configPath;
        if (string.IsNullOrEmpty(customDirectoryOrFile)) configPath = File.Exists(customDirectoryOrFile) ? customDirectoryOrFile : GetFilePath("Config.json", customDirectoryOrFile);
        else configPath = GetFilePath("Config.json");

        if (configPath == string.Empty)
        {
            WriteLineWithStepPretext("Config.json not found. Pulling Default from Github!", CurrentStep.FileReading, OutputType.Warning);
            if (string.IsNullOrEmpty(customDirectoryOrFile)) { await CreateConfigFile(); configPath = GetFilePath("Config.json"); }
            else { WriteLineWithStepPretext("Custom File or Directory specified, but unable to find Config File there!", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true); return null; }
            if (configPath == string.Empty) { WriteLineWithStepPretext("Unable to Find Config.json!", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true); return null; }
        }

        try
        {
            var configJson = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<Config>(configJson);

            // --- ENV VAR OVERRIDES (Syncs with Pelican Panel) ---
            string? val;

            val = Environment.GetEnvironmentVariable("MessageFormat");
            if (!string.IsNullOrEmpty(val) && Enum.TryParse(val, true, out MessageFormat mf)) config.MessageFormat = mf;

            val = Environment.GetEnvironmentVariable("MessageSorting");
            if (!string.IsNullOrEmpty(val) && Enum.TryParse(val, true, out MessageSorting ms)) config.MessageSorting = ms;

            val = Environment.GetEnvironmentVariable("MessageSortingDirection");
            if (!string.IsNullOrEmpty(val)) config.MessageSortingDirection = val;

            // Bools
            val = Environment.GetEnvironmentVariable("IgnoreOfflineServers");
            if (!string.IsNullOrEmpty(val)) config.IgnoreOfflineServers = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("IgnoreInternalServers");
            if (!string.IsNullOrEmpty(val)) config.IgnoreInternalServers = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("JoinableIpDisplay");
            if (!string.IsNullOrEmpty(val)) config.JoinableIpDisplay = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("PlayerCountDisplay");
            if (!string.IsNullOrEmpty(val)) config.PlayerCountDisplay = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("AutomaticShutdown");
            if (!string.IsNullOrEmpty(val)) config.AutomaticShutdown = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("AllowUserServerStartup");
            if (!string.IsNullOrEmpty(val)) config.AllowUserServerStartup = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("AllowUserServerStopping");
            if (!string.IsNullOrEmpty(val)) config.AllowUserServerStopping = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("AutoUpdate");
            if (!string.IsNullOrEmpty(val)) config.AutoUpdate = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("Debug");
            if (!string.IsNullOrEmpty(val)) config.Debug = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("DryRun");
            if (!string.IsNullOrEmpty(val)) config.DryRun = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("LimitServerCount");
            if (!string.IsNullOrEmpty(val)) config.LimitServerCount = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("ContinuesMarkdownRead");
            if (!string.IsNullOrEmpty(val)) config.ContinuesMarkdownRead = (val == "1" || val.ToLower() == "true");

            val = Environment.GetEnvironmentVariable("ContinuesGamesToMonitorRead");
            if (!string.IsNullOrEmpty(val)) config.ContinuesGamesToMonitorRead = (val == "1" || val.ToLower() == "true");

            // Strings & Arrays
            val = Environment.GetEnvironmentVariable("InternalIpStructure");
            if (!string.IsNullOrEmpty(val)) config.InternalIpStructure = val;

            val = Environment.GetEnvironmentVariable("EmptyServerTimeout");
            if (!string.IsNullOrEmpty(val)) config.EmptyServerTimeout = val;

            val = Environment.GetEnvironmentVariable("ServersToIgnore");
            if (!string.IsNullOrEmpty(val)) config.ServersToIgnore = val.Split(',');

            val = Environment.GetEnvironmentVariable("ServersToMonitor");
            if (!string.IsNullOrEmpty(val)) config.ServersToMonitor = val.Split(',');

            val = Environment.GetEnvironmentVariable("ServersToAutoShutdown");
            if (!string.IsNullOrEmpty(val)) config.ServersToAutoShutdown = val.Split(',');

            val = Environment.GetEnvironmentVariable("AllowServerStartup");
            if (!string.IsNullOrEmpty(val)) config.AllowServerStartup = val.Split(',');

            val = Environment.GetEnvironmentVariable("AllowServerStopping");
            if (!string.IsNullOrEmpty(val)) config.AllowServerStopping = val.Split(',');

            val = Environment.GetEnvironmentVariable("UsersAllowedToStartServers");
            if (!string.IsNullOrEmpty(val)) config.UsersAllowedToStartServers = val.Split(',');

            val = Environment.GetEnvironmentVariable("UsersAllowedToStopServers");
            if (!string.IsNullOrEmpty(val)) config.UsersAllowedToStopServers = val.Split(',');

            val = Environment.GetEnvironmentVariable("ServersToDisplay");
            if (!string.IsNullOrEmpty(val)) config.ServersToDisplay = val.Split(',');

            // Ints
            val = Environment.GetEnvironmentVariable("MarkdownUpdateInterval");
            if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int mui)) config.MarkdownUpdateInterval = mui;

            val = Environment.GetEnvironmentVariable("ServerUpdateInterval");
            if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int sui)) config.ServerUpdateInterval = sui;

            val = Environment.GetEnvironmentVariable("MaxServerCount");
            if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int msc)) config.MaxServerCount = msc;
            // ----------------------------------------------------

            Validator.ValidateConfig(config);
            if (config == null) { WriteLineWithStepPretext("Config file invalid.", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true); return null; }
            Program.Config = config;
            return config;
        }
        catch (Exception ex) { WriteLineWithStepPretext("Failed to load config.", CurrentStep.FileReading, OutputType.Error, ex, true); return null; }
    }

    public static async Task<List<GamesToMonitor>?> ReadGamesToMonitorFile(string? customDirectoryOrFile = null)
    {
        string gameCommPath;
        if (string.IsNullOrEmpty(customDirectoryOrFile)) gameCommPath = File.Exists(customDirectoryOrFile) ? customDirectoryOrFile : GetFilePath("GamesToMonitor.json", customDirectoryOrFile);
        else gameCommPath = GetFilePath("GamesToMonitor.json");

        if (gameCommPath == string.Empty)
        {
            WriteLineWithStepPretext("GamesToMonitor.json not found. Pulling from Github Repo!", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true);
            if (string.IsNullOrEmpty(customDirectoryOrFile)) { await CreateGamesToMonitorFile(); gameCommPath = GetFilePath("GamesToMonitor.json"); }
            else { WriteLineWithStepPretext("Custom File or Directory specified, but unable to find GamesToMonitor File there!", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true); return null; }
            if (gameCommPath == string.Empty) { WriteLineWithStepPretext("Unable to Find GamesToMonitor.json!", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true); return null; }
        }

        try
        {
            var gameCommJson = await File.ReadAllTextAsync(gameCommPath);
            var gameComms = JsonSerializer.Deserialize<List<GamesToMonitor>>(gameCommJson);
            return gameComms;
        }
        catch (Exception ex) { WriteLineWithStepPretext("Failed to load GamesToMonitor.json.", CurrentStep.FileReading, OutputType.Error, ex); return null; }
    }
}