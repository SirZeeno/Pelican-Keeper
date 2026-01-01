using Pelican_Keeper.Core;
using Pelican_Keeper.Query;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Tests;

/// <summary>
/// Tests for game server query services.
/// </summary>
[Category("Integration")]
public class QueryTesting
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

    /// <summary>
    /// Tests A2S query to a game server.
    /// Configure IP and port for your test server.
    /// </summary>
    [Test, Explicit("Requires real game server")]
    public async Task SendA2SQuery()
    {
        var testIp = RuntimeContext.Secrets.ExternalServerIp;

        try
        {
            var response = await A2SQueryService.QueryAsync(testIp!, 27051);

            if (string.IsNullOrEmpty(response) || response == "N/A")
                Assert.Fail("Empty response from A2S query.");

            Assert.Pass($"A2S query successful: {response}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"A2S query failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests RCON query to a game server.
    /// Configure IP, port, and password for your test server.
    /// </summary>
    [Test, Explicit("Requires real game server")]
    public async Task SendRconQuery()
    {
        var testIp = RuntimeContext.Secrets.ExternalServerIp;

        const string password = "YourRconPassword";
        const string command = "status";
        const int port = 27015;

        var service = new RconQueryService(testIp!, port, password);

        try
        {
            await service.ConnectAsync();
            var response = await service.QueryAsync(command, null);

            if (string.IsNullOrEmpty(response) || response == "N/A")
                Assert.Fail("Empty response from RCON query.");

            Assert.Pass($"RCON query successful: {response}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"RCON query failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests Minecraft Bedrock query to a game server.
    /// </summary>
    [Test, Explicit("Requires real Minecraft Bedrock server")]
    public async Task SendBedrockQuery()
    {
        var testIp = RuntimeContext.Secrets.ExternalServerIp;
        var service = new MinecraftBedrockQueryService(testIp!, 19132);

        try
        {
            await service.ConnectAsync();
            var response = await service.QueryAsync();

            if (string.IsNullOrEmpty(response) || response == "N/A")
                Assert.Fail("Empty response from Bedrock query.");

            Assert.Pass($"Bedrock query successful: {response}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Bedrock query failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests Minecraft Java query to a game server.
    /// </summary>
    [Test, Explicit("Requires real Minecraft Java server")]
    public async Task SendJavaQuery()
    {
        var testIp = RuntimeContext.Secrets.ExternalServerIp;
        var service = new MinecraftJavaQueryService(testIp!, 25565);

        try
        {
            await service.ConnectAsync();
            var response = await service.QueryAsync();

            if (string.IsNullOrEmpty(response) || response == "N/A")
                Assert.Fail("Empty response from Java query.");

            Assert.Pass($"Java query successful: {response}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Java query failed: {ex.Message}");
        }
    }
}
