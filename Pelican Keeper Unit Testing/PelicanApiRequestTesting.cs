using Pelican_Keeper;
using RestSharp;
using System.Text.Json;
using Pelican_Keeper.Helper_Classes;

namespace Pelican_Keeper_Unit_Testing;

public class PelicanApiRequestTesting
{
    private List<TemplateClasses.ServerInfo>? _serverInfos;
    
    [SetUp]
    public async Task Setup()
    {
        ConsoleExt.SuppressProcessExitForTests = true;
        var secrets = await FileManager.ReadSecretsFile();
        if (secrets != null) Program.Secrets = secrets;
        Program.Config = TestConfigCreator.CreateDefaultConfigInstance();
    }
    
    
    [Test, Order(1)]
    public void CanReachServer()
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/application/nodes/1");
        var response = HelperClass.CreateRequest(client, Program.Secrets.ClientToken);

        if (response.IsSuccessful) Assert.Pass();
        else Assert.Fail("Failed to reach server");
    }
    
    [Test, Order(2)]
    public void GetServerList()
    {
        _serverInfos = PelicanInterface.GetServersList();
        if (_serverInfos.Count > 0) Assert.Pass();
        else Assert.Fail("Failed to get Game servers");
    }
    
    [Test, Order(3)]
    public void GetServerResources()
    {
        if (_serverInfos == null) Assert.Fail("Server info list is null");
        
        PelicanInterface.GetServerResources(_serverInfos![0]);
        if (_serverInfos[0].Resources != null) Assert.Pass();
        else Assert.Fail("Server resources are null");
    }
    
    [Test, Order(4)]
    public async Task GetServerResourcesList()
    {
        if (_serverInfos == null) Assert.Fail("Server info list is null");
        
        await PelicanInterface.GetServerResourcesList(_serverInfos!);
        if (_serverInfos![1].Resources != null) Assert.Pass();
        else Assert.Fail("Server resources are null");
    }
    
    [Test, Order(5)]
    public void GetAllocationsList()
    {
        if (_serverInfos == null) Assert.Fail("Server info list is null");
        
        PelicanInterface.GetServerAllocations(_serverInfos!);
        if (_serverInfos![0].Allocations != null) Assert.Pass();
        else Assert.Fail("Server resources are null");
    }
}