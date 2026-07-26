using Newtonsoft.Json;
using Pelican_Keeper;

namespace Pelican_Keeper_Unit_Testing;

public class FileReadingTesting
{
    private string? _configFilePath;
    private string? _gamesToMonitorFilePath;
    private string? _messageHistoryFilePath;
    private string? _secretsFilePath;

    [SetUp]
    public void Setup()
    {
        ConsoleExt.SuppressProcessExitForTests = true;

        var directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
        if (directoryInfo.Parent?.Parent?.Parent?.Parent?.Parent?.Exists != false)
            directoryInfo = directoryInfo.Parent?.Parent?.Parent?.Parent?.Parent;

        var pelicanKeeperExists = false;
        if (directoryInfo == null) return;

        var childDirectories = directoryInfo.GetDirectories();
        foreach (var childDirectory in childDirectories)
            if (childDirectory.Name == "Pelican Keeper")
                pelicanKeeperExists = true;

        if (pelicanKeeperExists)
            directoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "Pelican Keeper"));

        var fileInfos = directoryInfo.GetFiles();
        foreach (var fileInfo in fileInfos)
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

    //TODO: also test iteratively how the program handles any of the config variables being wrong or null
    [Test]
    public async Task ReadingConfig()
    {
        var config = await FileManager.ReadConfigFile(_configFilePath);
        if (config == null || ConsoleExt.ExceptionOccurred)
            Assert.Fail($"Config file failed to read.\n Exception(s): {ConsoleExt.Exceptions}");
        else Assert.Pass("Config file read successfully.\n");
    }

    public static IEnumerable<TestCaseData> ConfigNullCases()
    {
        return TestingHelperClass.NullPropertyCases(TestConfigCreator.CreateDefaultConfigInstance);
    }

    [TestCaseSource(nameof(ConfigNullCases))]
    public async Task TestConfigScenarios(TemplateClasses.Config config)
    {
        var
            configJson =
                JsonConvert.SerializeObject(
                    config); //Stop testing the serialized when the things i want to test is being able to run the bot even if you misspelled something or put a wrong ID or value on the config
        await File.WriteAllTextAsync("./TestConfig.json", configJson);
        Assert.DoesNotThrowAsync(() =>
            FileManager.ReadConfigFile(
                "./TestConfig.json")); //TODO: change this to actually run the bot and checks for exceptions or throws
    }

    //TODO: also test iteratively how the program handles any of the secret variables being wrong or null
    [Test]
    public async Task ReadingSecrets()
    {
        var secrets = await FileManager.ReadSecretsFile(_secretsFilePath);
        if (secrets == null || ConsoleExt.ExceptionOccurred) Assert.Fail("Secrets file failed to read.\n");
        else Assert.Pass("Secrets file read successfully.\n");
    }

    //TODO: also test iteratively how the program handles any of the games to monitor variables being wrong or null
    [Test]
    public async Task ReadingGamesToMonitor()
    {
        var gamesToMonitor = await FileManager.ReadGamesToMonitorFile(_gamesToMonitorFilePath);
        if (gamesToMonitor == null || ConsoleExt.ExceptionOccurred)
            Assert.Fail("GamesToMonitor file failed to read.\n");
        else Assert.Pass($"GamesToMonitor file read successfully. Supported Games count: {gamesToMonitor.Count}\n");
    }
}