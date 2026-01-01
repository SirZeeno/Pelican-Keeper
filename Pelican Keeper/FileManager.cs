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
            Validator.ValidateConfig(config);
            if (config == null) { WriteLineWithStepPretext("Config file invalid.", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true); return null; }

            // --- ENV VAR OVERRIDES ---
            // Helper to get raw env var (returns null if missing, string if present)
            string? GetEnv(string key) => Environment.GetEnvironmentVariable(key);

            string? val;

            val = GetEnv("MessageFormat");
            if (!string.IsNullOrEmpty(val) && Enum.TryParse(val, true, out MessageFormat mf)) config.MessageFormat = mf;

            val = GetEnv("MessageSorting");
            if (!string.IsNullOrEmpty(val) && Enum.TryParse(val, true, out MessageSorting ms)) config.MessageSorting = ms;

            val = GetEnv("MessageSortingDirection");
            if (!string.IsNullOrEmpty(val) && Enum.TryParse(val, true, out MessageSortingDirection msd)) config.MessageSortingDirection = msd;

            // Bools
            val = GetEnv("IgnoreOfflineServers");
            if (!string.IsNullOrEmpty(val)) config.IgnoreOfflineServers = (val == "1" || val.ToLower() == "true");

            val = GetEnv("IgnoreInternalServers");
            if (!string.IsNullOrEmpty(val)) config.IgnoreInternalServers = (val == "1" || val.ToLower() == "true");

            val = GetEnv("JoinableIpDisplay");
            if (!string.IsNullOrEmpty(val)) config.JoinableIpDisplay = (val == "1" || val.ToLower() == "true");

            val = GetEnv("PlayerCountDisplay");
            if (!string.IsNullOrEmpty(val)) config.PlayerCountDisplay = (val == "1" || val.ToLower() == "true");

            val = GetEnv("AutomaticShutdown");
            if (!string.IsNullOrEmpty(val)) config.AutomaticShutdown = (val == "1" || val.ToLower() == "true");

            val = GetEnv("AllowUserServerStartup");
            if (!string.IsNullOrEmpty(val)) config.AllowUserServerStartup = (val == "1" || val.ToLower() == "true");

            val = GetEnv("AllowUserServerStopping");
            if (!string.IsNullOrEmpty(val)) config.AllowUserServerStopping = (val == "1" || val.ToLower() == "true");

            val = GetEnv("AutoUpdate");
            if (!string.IsNullOrEmpty(val)) config.AutoUpdate = (val == "1" || val.ToLower() == "true");

            val = GetEnv("Debug");
            if (!string.IsNullOrEmpty(val)) config.Debug = (val == "1" || val.ToLower() == "true");

            val = GetEnv("DryRun");
            if (!string.IsNullOrEmpty(val)) config.DryRun = (val == "1" || val.ToLower() == "true");

            val = GetEnv("LimitServerCount");
            if (!string.IsNullOrEmpty(val)) config.LimitServerCount = (val == "1" || val.ToLower() == "true");

            val = GetEnv("ContinuesMarkdownRead");
            if (!string.IsNullOrEmpty(val)) config.ContinuesMarkdownRead = (val == "1" || val.ToLower() == "true");

            val = GetEnv("ContinuesGamesToMonitorRead");
            if (!string.IsNullOrEmpty(val)) config.ContinuesGamesToMonitorRead = (val == "1" || val.ToLower() == "true");

            // Strings & Arrays
            // FIX: If Env Var exists but is empty, clear the list/string (overriding config.json defaults)

            val = GetEnv("InternalIpStructure");
            if (val != null) config.InternalIpStructure = val; // Allows setting to empty string

            val = GetEnv("EmptyServerTimeout");
            if (val != null) config.EmptyServerTimeout = val;

            val = GetEnv("ServersToIgnore");
            if (val != null) config.ServersToIgnore = string.IsNullOrWhiteSpace(val) ? [] : val.Split(',');

            val = GetEnv("ServersToMonitor");
            if (val != null) config.ServersToMonitor = string.IsNullOrWhiteSpace(val) ? [] : val.Split(',');

            val = GetEnv("ServersToAutoShutdown");
            if (val != null) config.ServersToAutoShutdown = string.IsNullOrWhiteSpace(val) ? [] : val.Split(',');

            val = GetEnv("AllowServerStartup");
            if (val != null) config.AllowServerStartup = string.IsNullOrWhiteSpace(val) ? [] : val.Split(',');

            val = GetEnv("AllowServerStopping");
            if (val != null) config.AllowServerStopping = string.IsNullOrWhiteSpace(val) ? [] : val.Split(',');

            val = GetEnv("UsersAllowedToStartServers");
            if (val != null) config.UsersAllowedToStartServers = string.IsNullOrWhiteSpace(val) ? [] : val.Split(',');

            val = GetEnv("UsersAllowedToStopServers");
            if (val != null) config.UsersAllowedToStopServers = string.IsNullOrWhiteSpace(val) ? [] : val.Split(',');

            val = GetEnv("ServersToDisplay");
            if (val != null) config.ServersToDisplay = string.IsNullOrWhiteSpace(val) ? [] : val.Split(',');

            // Ints
            val = GetEnv("MarkdownUpdateInterval");
            if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int mui)) config.MarkdownUpdateInterval = mui;

            val = GetEnv("ServerUpdateInterval");
            if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int sui)) config.ServerUpdateInterval = sui;

            val = GetEnv("MaxServerCount");
            if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int msc)) config.MaxServerCount = msc;

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