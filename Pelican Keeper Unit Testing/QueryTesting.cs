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
    }
    
    [Test]
    public async Task SendA2SQuery()
    {
        if (_secrets == null || _config == null)
        {
            Assert.Fail("Secrets or Config not loaded properly.");
            return;
        }

        if (_secrets.ExternalServerIp != null)
            await PelicanInterface.SendA2SRequest(_secrets.ExternalServerIp, 27051);
        
        if (ConsoleExt.ExceptionOccurred) //TODO: expand the testing to include output testing, to see if the output is as desired. For example i could get "Timed out waiting for server response." but the test returns a pass
            Assert.Fail($"Test failed due to exception(s): {ConsoleExt.Exceptions}");
        else
            Assert.Pass("A2S request sent successfully.");
    }
    
    [Test]
    public async Task SendRconQuery()
    {
        if (_secrets == null || _config == null)
        {
            Assert.Fail("Secrets or Config not loaded properly.");
            return;
        }

        if (_secrets.ExternalServerIp != null)
            await PelicanInterface.SendRconGameServerCommand(_secrets.ExternalServerIp, 7777, "YouSuck", "listplayers"); // should load these from the secrets file and the information provided by the pelican API

        if (ConsoleExt.ExceptionOccurred)
            Assert.Fail($"Test failed due to exception(s): {ConsoleExt.Exceptions}");
        else
            Assert.Pass("Rcon command sent successfully.");
    }

    [Test]
    public async Task SendBedrockQuery()
    {
        if (_secrets == null || _config == null)
        {
            Assert.Fail("Secrets or Config not loaded properly.");
            return;
        }

        if (_secrets.ExternalServerIp != null)
        {
            await PelicanInterface.SendBedrockMinecraftRequest(_secrets.ExternalServerIp, 19132);
        }
        
        if (ConsoleExt.ExceptionOccurred)
            Assert.Fail($"Test failed due to exception(s): {ConsoleExt.Exceptions}");
        else
            Assert.Pass("Rcon command sent successfully.");
    }
    
    [Test]
    public async Task SendJavaQuery()
    {
        if (_secrets == null || _config == null)
        {
            Assert.Fail("Secrets or Config not loaded properly.");
            return;
        }

        if (_secrets.ExternalServerIp != null)
            await PelicanInterface.SendJavaMinecraftRequest(_secrets.ExternalServerIp, 1251);
        
        if (ConsoleExt.ExceptionOccurred)
            Assert.Fail($"Test failed due to exception(s): {ConsoleExt.Exceptions}");
        else
            Assert.Pass("Rcon command sent successfully.");
    }
}