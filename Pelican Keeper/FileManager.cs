using System.Text.Json;

namespace Pelican_Keeper;

using Validators;
using Helper_Classes;
using static ConsoleExt;
using static TemplateClasses;

public static class FileManager
{
    /// <summary>
    ///     Gets the file path if it exists in the current execution directory or in the Pelican Keeper directory.
    /// </summary>
    /// <param name="fileNameWithExtension">The File name with Extension to check</param>
    /// <param name="originDirectory">Origin Directory to look in</param>
    /// <returns>The File path or empty string</returns>
    public static string GetFilePath(string fileNameWithExtension, string? originDirectory = null)
    {
        if (File.Exists(fileNameWithExtension)) return fileNameWithExtension;
        originDirectory ??= Environment.CurrentDirectory;

        foreach (var file in
                 Directory.GetFiles(originDirectory, fileNameWithExtension, SearchOption.AllDirectories)) return file;

        WriteLine($"Couldn't find {fileNameWithExtension} file in program directory!", CurrentStep.FileReading,
            OutputType.Error, new FileNotFoundException());
        return string.Empty;
    }

    /// <summary>
    ///     Creates a default Secrets.json file in the current execution directory.
    /// </summary>
    private static async Task CreateSecretsFile(string? customDirectoryOrFile = null)
    {
        WriteLine("Secrets.json not found. Creating default one.", CurrentStep.FileReading, OutputType.Warning);
        await using var secretsFile = PreliminaryCustomChecks("Secrets.json", customDirectoryOrFile);
        var defaultSecrets = new string(
            "{\n  \"ClientToken\": \"YOUR_CLIENT_TOKEN\",\n  \"ServerToken\": \"YOUR_SERVER_TOKEN\",\n  \"ServerUrl\": \"YOUR_BASIC_SERVER_URL\",\n  \"BotToken\": \"YOUR_DISCORD_BOT_TOKEN\",\n  \"ChannelIds\": [THE_CHANNEL_ID_YOU_WANT_THE_BOT_TO_POST_IN],\n  \"ExternalServerIp\": \"YOUR_EXTERNAL_SERVER_IP\"\n}");
        await using var writer = new StreamWriter(secretsFile);
        await writer.WriteAsync(defaultSecrets);
        WriteLine("Created default Secrets.json. Please fill out the values.", CurrentStep.FileReading,
            OutputType.Warning);
    }

    /// <summary>
    ///     Creates a default Config.json file in the current execution directory.
    /// </summary>
    private static async Task CreateConfigFile(string? customDirectoryOrFile = null)
    {
        await using var configFile = PreliminaryCustomChecks("Config.json", customDirectoryOrFile);
        var defaultConfig = await HelperClass.GetJsonTextAsync(
            "https://raw.githubusercontent.com/SirZeeno/Pelican-Keeper/refs/heads/testing/Pelican%20Keeper/Config.json");
        await using var writer = new StreamWriter(configFile);
        await writer.WriteAsync(defaultConfig);
    }

    /// <summary>
    ///     Checks if the custom location is a file or directory and creates a file as needed in the custom location or default location
    /// </summary>
    /// <param name="fileName">File Name with Extension</param>
    /// <param name="customDirectoryOrFile">Custom Directory or File</param>
    /// <returns>File Stream of the created file</returns>
    private static FileStream PreliminaryCustomChecks(string fileName, string? customDirectoryOrFile = null)
    {
        if (string.IsNullOrEmpty(customDirectoryOrFile)) return File.Create(fileName);
        FileAttributes atrributes = File.GetAttributes(customDirectoryOrFile);
        return File.Create(atrributes.HasFlag(FileAttributes.Directory) ? Path.Combine(customDirectoryOrFile, fileName) : customDirectoryOrFile);
    }

    private static async Task CreateGamesToMonitorFile(string? customDirectoryOrFile = null)
    {
        await using var gamesToMonitorFile = PreliminaryCustomChecks("GamesToMonitor.json", customDirectoryOrFile);
        var gamesToMonitor = await HelperClass.GetJsonTextAsync(
            "https://raw.githubusercontent.com/SirZeeno/Pelican-Keeper/refs/heads/testing/Pelican%20Keeper/GamesToMonitor.json");
        await using var writer = new StreamWriter(gamesToMonitorFile);
        await writer.WriteAsync(gamesToMonitor);
    }

    /// <summary>
    ///     Creates a default MessageMarkdown.txt file in the current execution directory.
    /// </summary>
    public static async Task CreateMessageMarkdownFile(string? customDirectoryOrFile = null)
    {
        await using var messageMarkdownFile = PreliminaryCustomChecks("MessageMarkdown.txt", customDirectoryOrFile);
        var defaultMarkdown = await HelperClass.GetJsonTextAsync(
            "https://raw.githubusercontent.com/SirZeeno/Pelican-Keeper/refs/heads/testing/Pelican%20Keeper/MessageMarkdown.txt");
        await using var writer = new StreamWriter(messageMarkdownFile);
        await writer.WriteAsync(defaultMarkdown);
    }

    /// <summary>
    ///     Reads the Secrets.json file and interprets it to the Secrets class structure.
    /// </summary>
    /// <returns>The interpreted Secrets in the Secrets class structure</returns>
    public static async Task<Secrets?> ReadSecretsFile(string? customDirectoryOrFile = null)
    {
        string secretsPath;

        if (!string.IsNullOrEmpty(customDirectoryOrFile))
        {
            FileAttributes attr = File.GetAttributes(customDirectoryOrFile);
            secretsPath = attr.HasFlag(FileAttributes.Directory) ? GetFilePath("Secrets.json", customDirectoryOrFile) : customDirectoryOrFile;
        }
        else
        {
            secretsPath = GetFilePath("Secrets.json");
        }

        if (secretsPath == string.Empty)
        {
            WriteLine("Secrets.json not found. Creating default one.", CurrentStep.FileReading, OutputType.Warning);

            await CreateSecretsFile(customDirectoryOrFile);
            secretsPath = GetFilePath("Secrets.json");

            if (secretsPath == string.Empty)
            {
                WriteLine("Unable to Find Secrets.json!", CurrentStep.FileReading, OutputType.Error,
                    new FileLoadException(), true);
                return null;
            }
        }

        try
        {
            var secretsJson = await File.ReadAllTextAsync(secretsPath);
            
            Secrets secrets = JsonSerializer.Deserialize<Secrets>(secretsJson)!; // Can never be null since it would throw an error if anything is wrong
            SecretsValidator.ValidateSecrets(secrets); //Validates the given information for possible issues before proceeding

            Program.Secrets = secrets;
            return secrets;
        }
        catch (Exception ex)
        {
            WriteLine(
                "Failed to load secrets. Check that the Secrets file is filled out and is in the correct format. Check Secrets.json",
                CurrentStep.FileReading, OutputType.Error, ex, true);
            return null;
        }
    }

    /// <summary>
    ///     Reads the Config.json file and interprets it to the Config class structure.
    /// </summary>
    /// <returns>The interpreted Config in the Config class structure</returns>
    public static async Task<Config?> ReadConfigFile(string? customDirectoryOrFile = null)
    {
        string configPath;

        if (!string.IsNullOrEmpty(customDirectoryOrFile))
        {
            FileAttributes attr = File.GetAttributes(customDirectoryOrFile);
            configPath = attr.HasFlag(FileAttributes.Directory) ? GetFilePath("Config.json", customDirectoryOrFile) : customDirectoryOrFile;
        }
        else
        {
            configPath = GetFilePath("Config.json");
        }

        if (configPath == string.Empty)
        {
            WriteLine("Config.json not found. Pulling Default from Github!", CurrentStep.FileReading,
                OutputType.Warning);

            await CreateConfigFile(customDirectoryOrFile);
            configPath = GetFilePath("Config.json");

            if (configPath == string.Empty)
            {
                WriteLine("Unable to Find Config.json!", CurrentStep.FileReading, OutputType.Error,
                    new FileLoadException(), true);
                return null;
            }
        }

        try
        {
            var configJson = await File.ReadAllTextAsync(configPath);
            Config config = JsonSerializer.Deserialize<Config>(configJson)!; // Can never be null since it would throw an error if anything is wrong
            ConfigValidator.ValidateConfig(config); //Validates the given information for possible issues before proceeding
            //TODO: All I need to check is the format of the values like the DateTime, Discord User IDs, etc

            Program.Config = config;
            return config;
        }
        catch (Exception ex)
        {
            WriteLine("Failed to load config. Check if nothing is misspelled and you used the correct options",
                CurrentStep.FileReading, OutputType.Error, ex, true);
            return null;
        }
    }

    /// <summary>
    ///     Reads the GamesToMonitor.json file and interprets it to the GamesToMonitor class structure.
    /// </summary>
    /// <returns>The interpreted GamesToMonitor in the GamesToMonitor class structure</returns>
    public static async Task<List<GamesToMonitor>?> ReadGamesToMonitorFile(string? customDirectoryOrFile = null)
    {
        string gameCommPath;

        if (!string.IsNullOrEmpty(customDirectoryOrFile))
        {
            FileAttributes attr = File.GetAttributes(customDirectoryOrFile);
            gameCommPath = attr.HasFlag(FileAttributes.Directory) ? GetFilePath("GamesToMonitor.json", customDirectoryOrFile) : customDirectoryOrFile;
        }
        else
        {
            gameCommPath = GetFilePath("GamesToMonitor.json");
        }

        if (gameCommPath == string.Empty)
        {
            WriteLine("GamesToMonitor.json not found. Pulling from Github Repo!", CurrentStep.FileReading,
                OutputType.Error, new FileLoadException(), true);
            
            await CreateGamesToMonitorFile(customDirectoryOrFile);
            gameCommPath = GetFilePath("GamesToMonitor.json");

            if (gameCommPath == string.Empty)
            {
                WriteLine("Unable to Find GamesToMonitor.json!", CurrentStep.FileReading, OutputType.Error,
                    new FileLoadException(), true);
                return null;
            }
        }

        try
        {
            var gameCommJson = await File.ReadAllTextAsync(gameCommPath);
            var gameComms = JsonSerializer.Deserialize<List<GamesToMonitor>>(gameCommJson);
            return gameComms;
        }
        catch (Exception ex)
        {
            WriteLine(
                "Failed to load GamesToMonitor.json. Check if nothing is misspelled and you used the correct options",
                CurrentStep.FileReading, OutputType.Error, ex);
            return null;
        }
    }
}