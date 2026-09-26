using Pelican_Keeper.Interfaces;

namespace Pelican_Keeper.Query_Services;

public class TShock(string ip, int port) : ISendCommand
{
    // To Anyone Reading this; this is not finalized or guaranteed to be implemented. This is not a priority, and I am still researching if this is worth implementing since, unless I am wrong, it's a protocol specifically for TShock
    public async Task Connect()
    {
        ConsoleExt.WriteLine("Terraria query protocol not implemented yet!", ConsoleExt.CurrentStep.GameMonitoring, ConsoleExt.OutputType.Error);
    }

    public async Task<string> SendCommandAsync(string? command, string? regexPattern)
    {
        ConsoleExt.WriteLine("Terraria query protocol not implemented yet!", ConsoleExt.CurrentStep.GameMonitoring, ConsoleExt.OutputType.Error);
        return "";
    }

    public void Dispose()
    {
        ConsoleExt.WriteLine("Terraria query protocol not implemented yet!", ConsoleExt.CurrentStep.GameMonitoring, ConsoleExt.OutputType.Error);
    }
}