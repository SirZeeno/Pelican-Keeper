using Pelican_Keeper;

namespace Pelican_Keeper_Unit_Testing;

public class FileReadingTesting
{
    private string? _configFilePath;
    private string? _secretsFilePath;
    private string? _messageHistoryFilePath;
    private string? _gamesToMonitorFilePath;
    
    [SetUp]
    public void Setup()
    {
        ConsoleExt.SuppressProcessExitForTests = true;
        
        DirectoryInfo? directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
        if (directoryInfo.Parent?.Parent?.Parent?.Parent?.Parent?.Exists != false) directoryInfo = directoryInfo.Parent?.Parent?.Parent?.Parent?.Parent;

        bool pelicanKeeperExists = false;
        if (directoryInfo == null) return;
        
        DirectoryInfo[] childDirectories = directoryInfo.GetDirectories();
        foreach (var childDirectory in childDirectories) if (childDirectory.Name == "Pelican Keeper") pelicanKeeperExists = true;

        if (pelicanKeeperExists) directoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "Pelican Keeper"));
        
        FileInfo[] fileInfos = directoryInfo.GetFiles();
        foreach (var fileInfo in fileInfos)
        {
            switch (fileInfo.Name)
            {
                case "Config.json":
                    _configFilePath = fileInfo.FullName;
                    break;
                case "Secrets.json":
                    _secretsFilePath = fileInfo.FullName;
                    break;
                case "MessageHistory.json":
                    _messageHistoryFilePath = fileInfo.FullName;
                    break;
                case "GamesToMonitor.json":
                    _gamesToMonitorFilePath = fileInfo.FullName;
                    break;
            }
        }
    }

    [Test]
    public async Task ReadingConfig()
    {
        TemplateClasses.Config? config = await FileManager.ReadConfigFile(_configFilePath);
        if (config == null || ConsoleExt.ExceptionOccurred) Assert.Fail("Config file failed to read.\n");
        else Assert.Pass("Config file read successfully.\n");
    }
    
    [Test]
    public async Task ReadingSecrets()
    {
        TemplateClasses.Secrets? secrets = await FileManager.ReadSecretsFile(_secretsFilePath);
        if (secrets == null || ConsoleExt.ExceptionOccurred) Assert.Fail("Secrets file failed to read.\n");
        else Assert.Pass("Secrets file read successfully.\n");
    }
    
    [Test]
    public void ReadingMessageHistory()
    {
        TemplateClasses.LiveMessageJsonStorage? liveMessageJsonStorage = LiveMessageStorage.LoadAll(_messageHistoryFilePath);
        if (liveMessageJsonStorage == null || ConsoleExt.ExceptionOccurred) Assert.Fail("LiveMessageJsonStorage file failed to read.\n");
        else Assert.Pass("LiveMessageJsonStorage file read successfully.\n");
    }
    
    [Test]
    public async Task ReadingGamesToMonitor()
    {
        List<TemplateClasses.GamesToMonitor>? gamesToMonitor = await FileManager.ReadGamesToMonitorFile(_gamesToMonitorFilePath);
        if (gamesToMonitor == null || ConsoleExt.ExceptionOccurred) Assert.Fail("GamesToMonitor file failed to read.\n");
        else Assert.Pass($"GamesToMonitor file read successfully. Supported Games count: {gamesToMonitor.Count}\n");
    }
}