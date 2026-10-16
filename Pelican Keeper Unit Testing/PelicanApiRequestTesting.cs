using Pelican_Keeper;
using Pelican_Keeper.Helper_Classes;
using Pelican_Keeper.Logging;
using RestSharp;

namespace Pelican_Keeper_Unit_Testing;

public class PelicanApiRequestTesting
{
    private Tuple<List<TemplateClasses.ServerInfo>, List<TemplateClasses.ServerInfo>> _serverInfos = null!;

    [SetUp]
    public async Task Setup()
    {
        ConsoleExt.SuppressProcessExitForTests = true;
        var secrets = await FileManager.ReadSecretsFile();
        if (secrets != null) Program.Secrets = secrets;
        Program.Config = TestConfigCreator.CreateDefaultConfigInstance();
    }


    [Test]
    public void CanReachServer()
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/application/nodes/1");
        var response = HelperClass.CreateRequest(client, Program.Secrets.ClientToken);

        if (response.IsSuccessful) Assert.Pass();
        else Assert.Fail("Failed to reach server");
    }

    [Test]
    [DependsOnTest("CanReachServer")]
    public void GetServerList()
    {
        _serverInfos = PelicanInterface.GetServersList();
        if (_serverInfos.Item1.Count > 0) Assert.Pass();
        else Assert.Fail("Failed to get Game servers");
    }

    [Test]
    [DependsOnTest("GetServerList")]
    public void GetServerResources()
    {
        if (_serverInfos == null) Assert.Fail("Server info list is null");

        PelicanInterface.GetServerResources(_serverInfos!.Item1[0]);
        if (_serverInfos.Item1[0].Resources != null) Assert.Pass();
        else Assert.Fail("Server resources are null");
    }

    [Test]
    [DependsOnTest("GetServerResources")]
    public async Task GetServerResourcesList()
    {
        if (_serverInfos == null) Assert.Fail("Server info list is null");

        await PelicanInterface.GetServerResourcesList(_serverInfos!.Item1);
        if (_serverInfos.Item1[1].Resources != null) Assert.Pass();
        else Assert.Fail("Server resources are null");
    }
}