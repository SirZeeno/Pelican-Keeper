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
    /// Loads Secrets from environment variables. No file needed.
    /// </summary>
    public static Task<Secrets?> ReadSecretsFileAsync(string? customPath = null)
    {
        // Build Secrets entirely from environment variables
        var secrets = ApplySecretsEnvironmentOverrides(null);

        try
        {
            Validator.ValidateSecrets(secrets);
            RuntimeContext.Secrets = secrets!;
            return Task.FromResult<Secrets?>(secrets);
        }
        catch (Exception ex)
        {
            Logger.WriteLineWithStep("Secrets validation failed. Check environment variables.", Logger.Step.FileReading, Logger.OutputType.Error, ex, shouldExit: true);
            return Task.FromResult<Secrets?>(null);
        }
    }

    /// <summary>
    /// Loads Config from environment variables with sensible defaults. No file needed.
    /// </summary>
    public static Task<Config?> ReadConfigFileAsync(string? customPath = null)
    {
        // Create config with defaults, then apply environment overrides
        var config = CreateDefaultConfig();
        ApplyEnvironmentOverrides(config);

        try
        {
            Validator.ValidateConfig(config);
            RuntimeContext.Config = config;
            return Task.FromResult<Config?>(config);
        }
        catch (Exception ex)
        {
            Logger.WriteLineWithStep("Config validation failed. Check environment variables.", Logger.Step.FileReading, Logger.OutputType.Error, ex, shouldExit: true);
            return Task.FromResult<Config?>(null);
        }
    }

    /// <summary>
    /// Creates a Config instance with sensible defaults.
    /// </summary>
    private static Config CreateDefaultConfig() => new()
    {
        InternalIpStructure = "192.168.*.*",
        MessageFormat = MessageFormat.Consolidated,
        MessageSorting = MessageSorting.Name,
        MessageSortingDirection = MessageSortingDirection.Ascending,
        IgnoreOfflineServers = false,
        IgnoreInternalServers = false,
        IgnoreServersWithoutAllocations = false,
        ServersToIgnore = [],
        JoinableIpDisplay = true,
        PlayerCountDisplay = true,
        ServersToMonitor = [],
        AutomaticShutdown = false,
        ServersToAutoShutdown = [],
        EmptyServerTimeout = "00:01:00",
        AllowUserServerStartup = true,
        AllowServerStartup = [],
        UsersAllowedToStartServers = [],
        AllowUserServerStopping = true,
        AllowServerStopping = [],
        UsersAllowedToStopServers = [],
        ContinuesMarkdownRead = false,
        ContinuesGamesToMonitorRead = false,
        MarkdownUpdateInterval = 30,
        ServerUpdateInterval = 10,
        LimitServerCount = false,
        MaxServerCount = 10,
        ServersToDisplay = [],
        Debug = false,
        DryRun = false,
        AutoUpdate = false,
        NotifyOnUpdate = true
    };

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
    /// Downloads GamesToMonitor.json from GitHub repository.
    /// </summary>
    public static async Task CreateGamesToMonitorFileAsync()
    {
        var url = RepoConfig.GetRawContentUrl("templates/GamesToMonitor.json");
        if (url == null)
        {
            Logger.WriteLineWithStep("Repository not configured. Set REPO_OWNER and REPO_NAME environment variables.", Logger.Step.FileReading, Logger.OutputType.Warning);
            return;
        }
        var content = await HttpHelper.GetStringAsync(url);
        await File.WriteAllTextAsync("GamesToMonitor.json", content);
    }

    /// <summary>
    /// Downloads MessageMarkdown.txt template from GitHub repository.
    /// </summary>
    public static async Task CreateMessageMarkdownFileAsync()
    {
        var url = RepoConfig.GetRawContentUrl("templates/MessageMarkdown.txt");
        if (url == null)
        {
            Logger.WriteLineWithStep("Repository not configured. Set REPO_OWNER and REPO_NAME environment variables.", Logger.Step.FileReading, Logger.OutputType.Warning);
            return;
        }
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
        if ((val = GetEnv("IgnoreServersWithoutAllocations")) != null) config.IgnoreServersWithoutAllocations = ParseBool(val);
        if ((val = GetEnv("JoinableIpDisplay")) != null) config.JoinableIpDisplay = ParseBool(val);
        if ((val = GetEnv("PlayerCountDisplay")) != null) config.PlayerCountDisplay = ParseBool(val);
        if ((val = GetEnv("AutomaticShutdown")) != null) config.AutomaticShutdown = ParseBool(val);
        if ((val = GetEnv("AllowUserServerStartup")) != null) config.AllowUserServerStartup = ParseBool(val);
        if ((val = GetEnv("AllowUserServerStopping")) != null) config.AllowUserServerStopping = ParseBool(val);
        // Support both AutoUpdate and AUTO_UPDATE (Pelican egg convention)
        if ((val = GetEnv("AutoUpdate") ?? GetEnv("AUTO_UPDATE")) != null) config.AutoUpdate = ParseBool(val);
        if ((val = GetEnv("NotifyOnUpdate")) != null) config.NotifyOnUpdate = ParseBool(val);
        if ((val = GetEnv("Debug")) != null) config.Debug = ParseBool(val);
        if ((val = GetEnv("DryRun")) != null) config.DryRun = ParseBool(val);
        if ((val = GetEnv("LimitServerCount")) != null) config.LimitServerCount = ParseBool(val);
        if ((val = GetEnv("ContinuesMarkdownRead")) != null) config.ContinuesMarkdownRead = ParseBool(val);
        if ((val = GetEnv("ContinuesGamesToMonitorRead")) != null) config.ContinuesGamesToMonitorRead = ParseBool(val);

        // Strings
        if ((val = GetEnv("InternalIpStructure")) != null) config.InternalIpStructure = val;
        if ((val = GetEnv("EmptyServerTimeout")) != null) config.EmptyServerTimeout = val;

        // String arrays (comma-separated, with whitespace trimming)
        static string[] ParseArray(string? v) => string.IsNullOrWhiteSpace(v)
            ? []
            : v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

    /// <summary>
    /// Applies environment variable overrides to secrets.
    /// Returns a new Secrets record with overridden values.
    /// Creates a default Secrets object if null to support environment-only configuration.
    /// </summary>
    private static Secrets ApplySecretsEnvironmentOverrides(Secrets? secrets)
    {
        // Create default if null - allows full configuration via environment variables
        secrets ??= new Secrets(null, null, null, null, null, null, null);

        static string? GetEnv(string key) => Environment.GetEnvironmentVariable(key);
        static ulong[]? ParseUlongArray(string? v) =>
            string.IsNullOrWhiteSpace(v)
                ? null
                : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => ulong.TryParse(s.Trim(), out var id) ? id : 0)
                    .Where(id => id != 0)
                    .ToArray();
        static ulong? ParseUlong(string? v) =>
            ulong.TryParse(v?.Trim(), out var id) ? id : null;

        return secrets with
        {
            ClientToken = GetEnv("ClientToken") ?? GetEnv("CLIENT_TOKEN") ?? secrets.ClientToken,
            ServerToken = GetEnv("ServerToken") ?? GetEnv("SERVER_TOKEN") ?? secrets.ServerToken,
            ServerUrl = GetEnv("ServerUrl") ?? GetEnv("SERVER_URL") ?? secrets.ServerUrl,
            BotToken = GetEnv("BotToken") ?? GetEnv("BOT_TOKEN") ?? secrets.BotToken,
            ChannelIds = ParseUlongArray(GetEnv("ChannelIds") ?? GetEnv("CHANNEL_IDS")) ?? secrets.ChannelIds,
            NotificationChannelId = ParseUlong(GetEnv("NotificationChannelId") ?? GetEnv("NOTIFICATION_CHANNEL_ID")) ?? secrets.NotificationChannelId,
            ExternalServerIp = GetEnv("ExternalServerIp") ?? GetEnv("EXTERNAL_SERVER_IP") ?? secrets.ExternalServerIp
        };
    }
}
