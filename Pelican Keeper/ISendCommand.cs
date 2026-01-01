namespace Pelican_Keeper;

public interface ISendCommand
{
    public Task Connect();

    public Task<string> SendCommandAsync(string command, string regexPattern);
}