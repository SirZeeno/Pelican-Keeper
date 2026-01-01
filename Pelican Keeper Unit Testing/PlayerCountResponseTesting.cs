using System.Text.RegularExpressions;
using Pelican_Keeper;
using Pelican_Keeper.Query_Services;

namespace Pelican_Keeper_Unit_Testing;

using static TemplateClasses;

public class PlayerCountResponseTesting
{
    private readonly string? _ip = null;
    private readonly int? _port = null;
    private readonly string? _password = null;
    private readonly string? _command = null;
    private readonly CommandExecutionMethod? _messageFormat = CommandExecutionMethod.A2S;

    [SetUp]
    public void Setup()
    {
        ConsoleExt.SuppressProcessExitForTests = true;
    }

    //[Test]
    public async Task PlayerCountResponseTest()
    {
        string? response = null;
        if (_ip != null && _port != null && _messageFormat != null)
        {
            switch (_messageFormat)
            {
                case CommandExecutionMethod.A2S:
                    {
                        A2SService a2S = new A2SService(_ip, (int)_port);

                        await a2S.Connect();
                        response = await a2S.SendCommandAsync();
                        break;
                    }
                case CommandExecutionMethod.Rcon:
                    {
                        if (_password != null && _command != null)
                        {
                            RconService rcon = new RconService(_ip, (int)_port, _password);
                            await rcon.Connect();
                            response = await rcon.SendCommandAsync(_command, null);
                        }
                        else
                        {
                            ConsoleExt.WriteLineWithPretext($"Password or Command is null. Password: {_password}, Command: {_command}");
                        }
                        break;
                    }
                case CommandExecutionMethod.MinecraftJava:
                    {
                        JavaMinecraftQueryService javaMinecraftQuery = new JavaMinecraftQueryService(_ip, (int)_port);

                        await javaMinecraftQuery.Connect();
                        response = await javaMinecraftQuery.SendCommandAsync();
                        break;
                    }
                case CommandExecutionMethod.MinecraftBedrock:
                    {
                        BedrockMinecraftQueryService bedrockMinecraftQuery = new BedrockMinecraftQueryService(_ip, (int)_port);

                        await bedrockMinecraftQuery.Connect();
                        response = await bedrockMinecraftQuery.SendCommandAsync();
                        break;
                    }
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (!string.IsNullOrEmpty(response))
        {
            var cleanResponse = HelperClass.ServerPlayerCountDisplayCleanup(response, 30);
            var playerMaxPlayer = Regex.Match(cleanResponse, @"^(\d+)\/\d+$");
            if (playerMaxPlayer.Success)
            {
                ConsoleExt.WriteLineWithPretext("Success! The Response Conforms to the output Standard!");
                ConsoleExt.WriteLineWithPretext($"Response: {response}");
                ConsoleExt.WriteLineWithPretext($"Clean Response: {cleanResponse}");
                Assert.Pass($"{response}, {cleanResponse}\n");
            }
            else
            {
                ConsoleExt.WriteLineWithPretext("Failed! The Response does not Conform to the output Standard!");
                ConsoleExt.WriteLineWithPretext($"Response: {response}");
                ConsoleExt.WriteLineWithPretext($"Clean Response: {cleanResponse}");
                Assert.Fail($"{response}, {cleanResponse}\n");
            }
        }
        else
        {
            Assert.Fail("Response not returned or Empty!\n");
        }
    }
}