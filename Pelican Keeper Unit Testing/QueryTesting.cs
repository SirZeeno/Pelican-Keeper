using Pelican_Keeper;

namespace Pelican_Keeper_Unit_Testing;

public class QueryTesting
{
    TemplateClasses.Secrets? _secrets;
    TemplateClasses.Config? _config;

    [SetUp]
    public async Task Setup()
    {
        ConsoleExt.SuppressProcessExitForTests = true;
        _secrets = await FileManager.ReadSecretsFile();
        _config = TestConfigCreator.CreateDefaultConfigInstance();

        Program.Config = _config;
    }

    [Test]
    public async Task SendA2SQuery()
    {
        if (_secrets == null || _config == null)
        {
            Assert.Fail("Secrets or Config not loaded properly.\n");
            return;
        }

        string? response = null;

        if (_secrets.ExternalServerIp != null)
            response = await PelicanInterface.SendA2SRequest(_secrets.ExternalServerIp, 27051);

        if (ConsoleExt.ExceptionOccurred) //TODO: expand the testing to include output testing, to see if the output is as desired. For example i could get "Timed out waiting for server response." but the test returns a pass
            Assert.Fail($"Test failed due to exception(s): {ConsoleExt.Exceptions}\n");
        if (string.IsNullOrEmpty(response) || response == "N/A")
            Assert.Fail("Test failed due to null or empty response from sending A2S command.\n");
        Assert.Pass("A2S request sent successfully.\n");
    }

    [Test]
    public async Task SendRconQuery()
    {
        if (_secrets == null || _config == null)
        {
            Assert.Fail("Secrets or Config not loaded properly.\n");
            return;
        }

        string? response = null;

        if (_secrets.ExternalServerIp != null)
            response = await PelicanInterface.SendRconGameServerCommand(_secrets.ExternalServerIp, 37015, "YouSuck", "listplayers"); // should load these from the secrets file and the information provided by the pelican API

        if (ConsoleExt.ExceptionOccurred)
            Assert.Fail($"Test failed due to exception(s): {ConsoleExt.Exceptions}\n");
        if (string.IsNullOrEmpty(response) || response == "N/A")
            Assert.Fail("Test failed due to null or empty response from sending RCON command.\n");
        Assert.Pass("Rcon command sent successfully.\n");
    }

    [Test]
    public async Task SendBedrockQuery()
    {
        if (_secrets == null || _config == null)
        {
            Assert.Fail("Secrets or Config not loaded properly.\n");
            return;
        }

        string? response = null;

        if (_secrets.ExternalServerIp != null)
            response = await PelicanInterface.SendBedrockMinecraftRequest(_secrets.ExternalServerIp, 19132);

        if (ConsoleExt.ExceptionOccurred)
            Assert.Fail($"Test failed due to exception(s): {ConsoleExt.Exceptions}\n");
        if (string.IsNullOrEmpty(response) || response == "N/A")
            Assert.Fail("Test failed due to null or empty response from sending Bedrock query command.\n");
        Assert.Pass("Bedrock Query command sent successfully.\n");
    }

    [Test]
    public async Task SendJavaQuery()
    {
        if (_secrets == null || _config == null)
        {
            Assert.Fail("Secrets or Config not loaded properly.\n");
            return;
        }

        string? response = null;

        if (_secrets.ExternalServerIp != null)
            response = await PelicanInterface.SendJavaMinecraftRequest(_secrets.ExternalServerIp, 1251);

        if (ConsoleExt.ExceptionOccurred)
            Assert.Fail($"Test failed due to exception(s): {ConsoleExt.Exceptions}\n");
        if (string.IsNullOrEmpty(response) || response == "N/A")
            Assert.Fail("Test failed due to null or empty response from sending Bedrock query command.\n");
        Assert.Pass("Java Query command sent successfully.\n");
    }
}