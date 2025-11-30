using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Pelican_Keeper;

using static ConsoleExt;
using static TemplateClasses;

public static class FileManager
{
    /// <summary>
    /// Gets the file path if it exists in the current execution directory or in the Pelican Keeper directory.
    /// </summary>
    /// <param name="fileNameWithExtension">The File name with Extension to check</param>
    /// <param name="originDirectory">Origin Directory to look in</param>
    /// <returns>The File path or empty string</returns>
    public static string GetFilePath(string fileNameWithExtension, string? originDirectory = null)
    {
        if (File.Exists(fileNameWithExtension))
        {
            return fileNameWithExtension;
        }

        if (string.IsNullOrEmpty(originDirectory))
        {
            originDirectory = Environment.CurrentDirectory;
        }
        
        foreach (var file in Directory.GetFiles(originDirectory, fileNameWithExtension, SearchOption.AllDirectories))
        {
            return file;
        }
        
        WriteLineWithStepPretext($"Couldn't find {fileNameWithExtension} file in program directory!", CurrentStep.FileReading, OutputType.Error, new FileNotFoundException());
        return string.Empty;
    }

    /// <summary>
    /// Creates a default Secrets.json file in the current execution directory.
    /// </summary>
    private static async Task CreateSecretsFile()
    {
        WriteLineWithStepPretext("Secrets.json not found. Creating default one.", CurrentStep.FileReading, OutputType.Warning);
        await using var secretsFile = File.Create("Secrets.json");
        string defaultSecrets = new string("{\n  \"ClientToken\": \"YOUR_CLIENT_TOKEN\",\n  \"ServerToken\": \"YOUR_SERVER_TOKEN\",\n  \"ServerUrl\": \"YOUR_BASIC_SERVER_URL\",\n  \"BotToken\": \"YOUR_DISCORD_BOT_TOKEN\",\n  \"ChannelIds\": [THE_CHANNEL_ID_YOU_WANT_THE_BOT_TO_POST_IN],\n  \"ExternalServerIp\": \"YOUR_EXTERNAL_SERVER_IP\"\n}");
        await using var writer = new StreamWriter(secretsFile);
        await writer.WriteAsync(defaultSecrets);
        WriteLineWithStepPretext("Created default Secrets.json. Please fill out the values.", CurrentStep.FileReading, OutputType.Warning);
    }

    /// <summary>
    /// Creates a default Config.json file in the current execution directory.
    /// </summary>
    private static async Task CreateConfigFile()
    {
        await using var configFile = File.Create("Config.json");
        var defaultConfig = HelperClass.GetJsonTextAsync("https://raw.githubusercontent.com/SirZeeno/Pelican-Keeper/refs/heads/testing/Pelican%20Keeper/Config.json").GetAwaiter().GetResult();
        await using var writer = new StreamWriter(configFile);
        await writer.WriteAsync(defaultConfig);
    }
    
    private static async Task CreateGamesToMonitorFile()
    {
        await using var gamesToMonitorFile = File.Create("GamesToMonitor.json");
        var gamesToMonitor = HelperClass.GetJsonTextAsync("https://raw.githubusercontent.com/SirZeeno/Pelican-Keeper/refs/heads/testing/Pelican%20Keeper/GamesToMonitor.json").GetAwaiter().GetResult();
        await using var writer = new StreamWriter(gamesToMonitorFile);
        await writer.WriteAsync(gamesToMonitor);
    }

    /// <summary>
    /// Creates a default MessageMarkdown.txt file in the current execution directory.
    /// </summary>
    public static async Task CreateMessageMarkdownFile()
    {
        await using var messageMarkdownFile = File.Create("MessageMarkdown.txt");
        var defaultMarkdown = HelperClass.GetJsonTextAsync("https://raw.githubusercontent.com/SirZeeno/Pelican-Keeper/refs/heads/testing/Pelican%20Keeper/MessageMarkdown.txt").GetAwaiter().GetResult();
        await using var writer = new StreamWriter(messageMarkdownFile);
        await writer.WriteAsync(defaultMarkdown);
    }
    
    /// <summary>
    /// Reads the Secrets.json file and interprets it to the Secrets class structure.
    /// </summary>
    /// <returns>The interpreted Secrets in the Secrets class structure</returns>
    public static async Task<Secrets?> ReadSecretsFile(string? customDirectory = null)
    {
        string secretsPath = GetFilePath("Secrets.json", customDirectory);
        
        if (secretsPath == String.Empty)
        {
            WriteLineWithStepPretext("Secrets.json not found. Creating default one.", CurrentStep.FileReading, OutputType.Warning);
            await CreateSecretsFile();
            return null;
        }

        Secrets? secrets;
        try
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                Error = (sender, args) =>
                {
                    // skip invalid values instead of throwing
                    args.ErrorContext.Handled = true;
                }
            };
            
            var secretsJson = await File.ReadAllTextAsync(secretsPath);
            
            secrets = JsonConvert.DeserializeObject<Secrets>(secretsJson, settings); // for ignoring errors when deserializing the secrets file, since I may edit the structure in the future and i want this to tell the user what changed.
            Validator.ValidateSecrets(secrets);
        }
        catch (Exception ex)
        {
            WriteLineWithStepPretext("Failed to load secrets. Check that the Secrets file is filled out and is in the correct format. Check Secrets.json", CurrentStep.FileReading, OutputType.Error, ex, true);
            return null;
        }

        Program.Secrets = secrets!;
        return secrets;
    }
    
    /// <summary>
    /// Reads the Config.json file and interprets it to the Config class structure.
    /// </summary>
    /// <returns>The interpreted Config in the Config class structure</returns>
    public static async Task<Config?> ReadConfigFile(string? customDirectory = null)
    {
        string configPath = GetFilePath("Config.json", customDirectory);
        
        if (configPath == String.Empty)
        {
            WriteLineWithStepPretext("Config.json not found. Pulling Default from Github!", CurrentStep.FileReading, OutputType.Warning);
            await CreateConfigFile();
        }
        
        Config? config;
        try
        {
            var configJson = await File.ReadAllTextAsync(configPath);
            config = JsonSerializer.Deserialize<Config>(configJson);
        }
        catch (Exception ex)
        {
            WriteLineWithStepPretext("Failed to load config. Check if nothing is misspelled and you used the correct options", CurrentStep.FileReading, OutputType.Error, ex, true);
            return null;
        }
        
        if (config == null)
        {
            WriteLineWithStepPretext("Config file is empty or not in the correct format. Please check Config.json", CurrentStep.FileReading, OutputType.Error, new FileLoadException(), true);
            return null;
        }
        
        Program.Config = config;
        return config;
    }
    
    /// <summary>
    /// Reads the GamesToMonitor.json file and interprets it to the GamesToMonitor class structure.
    /// </summary>
    /// <returns>The interpreted GamesToMonitor in the GamesToMonitor class structure</returns>
    public static async Task<List<GamesToMonitor>?> ReadGamesToMonitorFile()
    {
        string gameCommPath = GetFilePath("GamesToMonitor.json");
        
        if (gameCommPath == String.Empty)
        {
            WriteLineWithStepPretext("GamesToMonitor.json not found. Pulling from Github Repo!", CurrentStep.FileReading, OutputType.Error);
            await CreateGamesToMonitorFile();
            return null;
        }

        try
        {
            var gameCommJson = await File.ReadAllTextAsync(gameCommPath);
            var gameComms = JsonSerializer.Deserialize<List<GamesToMonitor>>(gameCommJson);
            return gameComms;
        }
        catch (Exception ex)
        {
            WriteLineWithStepPretext("Failed to load GamesToMonitor.json. Check if nothing is misspelled and you used the correct options", CurrentStep.FileReading, OutputType.Error, ex);
            return null;
        }
    }
}