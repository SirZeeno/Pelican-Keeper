using Newtonsoft.Json;
using Pelican_Keeper.Core;
using Pelican_Keeper.Models;
using Pelican_Keeper.Utilities;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Pelican_Keeper.Configuration;

/// <summary>
/// Handles file I/O operations for configuration files and templates.
/// Creates default files from GitHub repository when missing.
/// </summary>
public static class FileManager
{
    /// <summary>
    /// Searches for a file starting from the current directory.
    /// </summary>
    /// <param name="fileName">File name with extension.</param>
    /// <param name="searchDirectory">Starting directory for search.</param>
    /// <returns>Full path if found, empty string otherwise.</returns>
    public static string GetFilePath(string fileName, string? searchDirectory = null)
    {
        if (File.Exists(fileName)) return fileName;
        searchDirectory ??= Environment.CurrentDirectory;

        foreach (var file in Directory.GetFiles(searchDirectory, fileName, SearchOption.AllDirectories))
            return file;

        Logger.WriteLineWithStep($"Could not find {fileName} in program directory.", Logger.Step.FileReading, Logger.OutputType.Error);
        return string.Empty;
    }

    /// <summary>
    /// Gets file path with optional custom directory override.
    /// </summary>
    public static string GetCustomFilePath(string fileName, string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
            return customPath;

        return GetFilePath(fileName, customPath);
    }

    /// <summary>
    /// Reads and parses Secrets.json, creating default if missing.
    /// </summary>
    public static async Task<Secrets?> ReadSecretsFileAsync(string? customPath = null)
    {
        var path = string.IsNullOrEmpty(customPath) || !File.Exists(customPath)
            ? GetFilePath("Secrets.json", customPath)
            : customPath;

        if (path == string.Empty)
        {
            Logger.WriteLineWithStep("Secrets.json not found. Creating default.", Logger.Step.FileReading, Logger.OutputType.Warning);

            if (!string.IsNullOrEmpty(customPath))
            {
                Logger.WriteLineWithStep("Custom path specified but file not found.", Logger.Step.FileReading, Logger.OutputType.Error, shouldExit: true);
                return null;
            }

            await CreateSecretsFileAsync();
            path = GetFilePath("Secrets.json");

            if (path == string.Empty)
            {
                Logger.WriteLineWithStep("Unable to find Secrets.json after creation.", Logger.Step.FileReading, Logger.OutputType.Error, shouldExit: true);
                return null;
            }
        }

        try
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                Error = (_, args) => args.ErrorContext.Handled = true
            };

            var json = await File.ReadAllTextAsync(path);
            var secrets = JsonConvert.DeserializeObject<Secrets>(json, settings);

            Validator.ValidateSecrets(secrets);
            RuntimeContext.Secrets = secrets!;
            return secrets;
        }
        catch (Exception ex)
        {
            Logger.WriteLineWithStep("Failed to load Secrets.json.", Logger.Step.FileReading, Logger.OutputType.Error, ex, shouldExit: true);
            return null;
        }
    }

    /// <summary>
    /// Reads and parses Config.json with environment variable overrides.
    /// </summary>
    public static async Task<Config?> ReadConfigFileAsync(string? customPath = null)
    {
        var path = string.IsNullOrEmpty(customPath) || !File.Exists(customPath)
            ? GetFilePath("Config.json", customPath)
            : customPath;

        if (path == string.Empty)
        {
            Logger.WriteLineWithStep("Config.json not found. Pulling from GitHub.", Logger.Step.FileReading, Logger.OutputType.Warning);

            if (!string.IsNullOrEmpty(customPath))
            {
                Logger.WriteLineWithStep("Custom path specified but file not found.", Logger.Step.FileReading, Logger.OutputType.Error, shouldExit: true);
                return null;
            }

            await CreateConfigFileAsync();
            path = GetFilePath("Config.json");

            if (path == string.Empty)
            {
                Logger.WriteLineWithStep("Unable to find Config.json after creation.", Logger.Step.FileReading, Logger.OutputType.Error, shouldExit: true);
                return null;
            }
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var config = JsonSerializer.Deserialize<Config>(json);

            Validator.ValidateConfig(config);
            ApplyEnvironmentOverrides(config!);

            RuntimeContext.Config = config!;
            return config;
        }
        catch (Exception ex)
        {
            Logger.WriteLineWithStep("Failed to load Config.json.", Logger.Step.FileReading, Logger.OutputType.Error, ex, shouldExit: true);
            return null;
        }
    }

    /// <summary>
    /// Reads GamesToMonitor.json for player count query configuration.
    /// </summary>
    public static async Task<List<GamesToMonitor>?> ReadGamesToMonitorFileAsync(string? customPath = null)
    {
        var path = string.IsNullOrEmpty(customPath) || !File.Exists(customPath)
            ? GetFilePath("GamesToMonitor.json", customPath)
            : customPath;

        if (path == string.Empty)
        {
            Logger.WriteLineWithStep("GamesToMonitor.json not found. Pulling from GitHub.", Logger.Step.FileReading, Logger.OutputType.Warning);

            if (!string.IsNullOrEmpty(customPath))
            {
                Logger.WriteLineWithStep("Custom path specified but file not found.", Logger.Step.FileReading, Logger.OutputType.Error, shouldExit: true);
                return null;
            }

            await CreateGamesToMonitorFileAsync();
            path = GetFilePath("GamesToMonitor.json");

            if (path == string.Empty)
            {
                Logger.WriteLineWithStep("Unable to find GamesToMonitor.json after creation.", Logger.Step.FileReading, Logger.OutputType.Error, shouldExit: true);
                return null;
            }
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<GamesToMonitor>>(json);
        }
        catch (Exception ex)
        {
            Logger.WriteLineWithStep("Failed to load GamesToMonitor.json.", Logger.Step.FileReading, Logger.OutputType.Error, ex);
            return null;
        }
    }

    /// <summary>
    /// Creates default Secrets.json template.
    /// </summary>
    public static async Task CreateSecretsFileAsync()
    {
        const string template = """
            {
              "ClientToken": "YOUR_CLIENT_TOKEN",
              "ServerToken": "YOUR_SERVER_TOKEN",
              "ServerUrl": "YOUR_BASIC_SERVER_URL",
              "BotToken": "YOUR_DISCORD_BOT_TOKEN",
              "ChannelIds": [THE_CHANNEL_ID_YOU_WANT_THE_BOT_TO_POST_IN],
              "ExternalServerIp": "YOUR_EXTERNAL_SERVER_IP"
            }
            """;

        await File.WriteAllTextAsync("Secrets.json", template);
        Logger.WriteLineWithStep("Created default Secrets.json. Please fill in the values.", Logger.Step.FileReading, Logger.OutputType.Warning);
    }

    /// <summary>
    /// Downloads Config.json template from GitHub repository.
    /// </summary>
    public static async Task CreateConfigFileAsync()
    {
        var url = RepoConfig.GetRawContentUrl("Pelican%20Keeper/Config.json");
        var content = await HttpHelper.GetStringAsync(url);
        await File.WriteAllTextAsync("Config.json", content);
    }

    /// <summary>
    /// Downloads GamesToMonitor.json from GitHub repository.
    /// </summary>
    public static async Task CreateGamesToMonitorFileAsync()
    {
        var url = RepoConfig.GetRawContentUrl("Pelican%20Keeper/GamesToMonitor.json");
        var content = await HttpHelper.GetStringAsync(url);
        await File.WriteAllTextAsync("GamesToMonitor.json", content);
    }

    /// <summary>
    /// Downloads MessageMarkdown.txt template from GitHub repository.
    /// </summary>
    public static async Task CreateMessageMarkdownFileAsync()
    {
        var url = RepoConfig.GetRawContentUrl("Pelican%20Keeper/MessageMarkdown.txt");
        var content = await HttpHelper.GetStringAsync(url);
        await File.WriteAllTextAsync("MessageMarkdown.txt", content);
    }

    /// <summary>
    /// Applies environment variable overrides to configuration.
    /// </summary>
    private static void ApplyEnvironmentOverrides(Config config)
    {
        static string? GetEnv(string key) => Environment.GetEnvironmentVariable(key);
        static bool ParseBool(string? val)
        {
            if (val == null)
                throw new System.ArgumentNullException(nameof(val), "Boolean environment value cannot be null.");

            if (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;

            if (val == "0" || val.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            throw new System.FormatException($"Invalid boolean value for environment override: '{val}'. Expected '1', '0', 'true', or 'false'.");
        }
        string? val;

        // Enums
        if ((val = GetEnv("MessageFormat")) != null && Enum.TryParse(val, true, out MessageFormat mf))
            config.MessageFormat = mf;
        if ((val = GetEnv("MessageSorting")) != null && Enum.TryParse(val, true, out MessageSorting ms))
            config.MessageSorting = ms;
        if ((val = GetEnv("MessageSortingDirection")) != null && Enum.TryParse(val, true, out MessageSortingDirection msd))
            config.MessageSortingDirection = msd;

        // Booleans
        if ((val = GetEnv("IgnoreOfflineServers")) != null) config.IgnoreOfflineServers = ParseBool(val);
        if ((val = GetEnv("IgnoreInternalServers")) != null) config.IgnoreInternalServers = ParseBool(val);
        if ((val = GetEnv("JoinableIpDisplay")) != null) config.JoinableIpDisplay = ParseBool(val);
        if ((val = GetEnv("PlayerCountDisplay")) != null) config.PlayerCountDisplay = ParseBool(val);
        if ((val = GetEnv("AutomaticShutdown")) != null) config.AutomaticShutdown = ParseBool(val);
        if ((val = GetEnv("AllowUserServerStartup")) != null) config.AllowUserServerStartup = ParseBool(val);
        if ((val = GetEnv("AllowUserServerStopping")) != null) config.AllowUserServerStopping = ParseBool(val);
        if ((val = GetEnv("AutoUpdate")) != null) config.AutoUpdate = ParseBool(val);
        if ((val = GetEnv("NotifyOnUpdate")) != null) config.NotifyOnUpdate = ParseBool(val);
        if ((val = GetEnv("Debug")) != null) config.Debug = ParseBool(val);
        if ((val = GetEnv("DryRun")) != null) config.DryRun = ParseBool(val);
        if ((val = GetEnv("LimitServerCount")) != null) config.LimitServerCount = ParseBool(val);
        if ((val = GetEnv("ContinuesMarkdownRead")) != null) config.ContinuesMarkdownRead = ParseBool(val);
        if ((val = GetEnv("ContinuesGamesToMonitorRead")) != null) config.ContinuesGamesToMonitorRead = ParseBool(val);

        // Strings
        if ((val = GetEnv("InternalIpStructure")) != null) config.InternalIpStructure = val;
        if ((val = GetEnv("EmptyServerTimeout")) != null) config.EmptyServerTimeout = val;

        // String arrays (comma-separated)
        static string[] ParseArray(string? v) => string.IsNullOrWhiteSpace(v) ? [] : v.Split(',');
        if ((val = GetEnv("ServersToIgnore")) != null) config.ServersToIgnore = ParseArray(val);
        if ((val = GetEnv("ServersToMonitor")) != null) config.ServersToMonitor = ParseArray(val);
        if ((val = GetEnv("ServersToAutoShutdown")) != null) config.ServersToAutoShutdown = ParseArray(val);
        if ((val = GetEnv("AllowServerStartup")) != null) config.AllowServerStartup = ParseArray(val);
        if ((val = GetEnv("AllowServerStopping")) != null) config.AllowServerStopping = ParseArray(val);
        if ((val = GetEnv("UsersAllowedToStartServers")) != null) config.UsersAllowedToStartServers = ParseArray(val);
        if ((val = GetEnv("UsersAllowedToStopServers")) != null) config.UsersAllowedToStopServers = ParseArray(val);
        if ((val = GetEnv("ServersToDisplay")) != null) config.ServersToDisplay = ParseArray(val);

        // Integers
        if ((val = GetEnv("MarkdownUpdateInterval")) != null && int.TryParse(val, out int mui))
            config.MarkdownUpdateInterval = mui;
        if ((val = GetEnv("ServerUpdateInterval")) != null && int.TryParse(val, out int sui))
            config.ServerUpdateInterval = sui;
        if ((val = GetEnv("MaxServerCount")) != null && int.TryParse(val, out int msc))
            config.MaxServerCount = msc;
    }
}
