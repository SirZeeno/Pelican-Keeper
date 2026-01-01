using Pelican_Keeper.Core;
using Pelican_Keeper.Pelican;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Tests;

/// <summary>
/// Tests for Pelican Panel API requests.
/// These tests require valid credentials in Secrets.json to run.
/// </summary>
[Category("Integration")]
public class PelicanApiRequestTesting
{
    [SetUp]
    public void Setup()
    {
        Logger.SuppressExitForTests = true;
        RuntimeContext.Config = TestConfigCreator.CreateDefaultConfigInstance();
        RuntimeContext.Secrets = TestConfigCreator.CreateDefaultSecretsInstance();
    }

    [TearDown]
    public void TearDown()
    {
        RuntimeContext.Reset();
    }

    [Test, Explicit("Requires real Pelican Panel credentials")]
    public void GetServerList()
    {
        try
        {
            var servers = PelicanApiClient.GetServerList();
            Assert.That(servers, Is.Not.Null, "Server list should be returned.");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to get server list: {ex.Message}");
        }
    }
}
