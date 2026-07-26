using System.Net.Sockets;
using System.Text;
using Pelican_Keeper.Helper_Classes;
using Pelican_Keeper.Interfaces;

namespace Pelican_Keeper.Query_Services;

public class RconService(string ip, int port, string password) : ISendCommand, IDisposable
{
    public readonly string Ip = ip;
    public readonly int Port = port;
    private int _requestId;
    private NetworkStream? _stream;
    private TcpClient? _tcpClient;

    public void Dispose()
    {
        _stream?.Dispose();
        _tcpClient?.Close();
    }

    public async Task Connect()
    {
        _tcpClient = new TcpClient();
        try
        {
            await _tcpClient.ConnectAsync(Ip, Port);
        }
        catch (Exception e)
        {
            ConsoleExt.WriteLine(e, ConsoleExt.CurrentStep.RconQuery, ConsoleExt.OutputType.Debug);
            return;
        }

        _stream = _tcpClient.GetStream();

        var authenticated = await AuthenticateAsync();
        if (authenticated)
            ConsoleExt.WriteLine("RCON connection established successfully.", ConsoleExt.CurrentStep.RconQuery,
                ConsoleExt.OutputType.Debug);
        else
            ConsoleExt.WriteLine("RCON authentication failed.", ConsoleExt.CurrentStep.RconQuery,
                ConsoleExt.OutputType.Error, new UnauthorizedAccessException());
    }

    public async Task<string> SendCommandAsync(string command, string? regexPattern)
    {
        if (_tcpClient == null || _stream == null)
        {
            ConsoleExt.WriteLine(new InvalidOperationException("Call Connect() before sending commands."),
                ConsoleExt.CurrentStep.RconQuery, ConsoleExt.OutputType.Debug);
            return ExtractorHelpers.ExtractPlayerCount(null, regexPattern).ToString();
        }

        _requestId++;
        var packet = CreatePacket(_requestId, 2, command);
        await _stream.WriteAsync(packet);

        var response = await ReadResponseAsync();
        ConsoleExt.WriteLine($"RCON command response: {response.body.Trim()}", ConsoleExt.CurrentStep.RconQuery,
            ConsoleExt.OutputType.Debug);
        return ExtractorHelpers.ExtractPlayerCount(response.body.Trim(), regexPattern).ToString();
    }

    private async Task<bool> AuthenticateAsync()
    {
        _requestId++;
        var packet = CreatePacket(_requestId, 3, password);
        await _stream!.WriteAsync(packet);

        var response = await ReadResponseAsync();
        return response.type == 2 && response.id == _requestId;
    }

    private byte[] CreatePacket(int id, int type, string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var packet = new byte[4 + 4 + bodyBytes.Length + 2]; // Size = Id + Type + Body + 2 null bytes

        BitConverter.GetBytes(id).CopyTo(packet, 0);
        BitConverter.GetBytes(type).CopyTo(packet, 4);
        bodyBytes.CopyTo(packet, 8);
        packet[^2] = 0; // Null terminator
        packet[^1] = 0; // Null terminator

        var fullPacket = new byte[4 + packet.Length]; // Full packet size = Length + packet
        BitConverter.GetBytes(packet.Length).CopyTo(fullPacket, 0);
        packet.CopyTo(fullPacket, 4);

        return fullPacket;
    }

    private async Task<(int id, int type, string body)> ReadResponseAsync()
    {
        var sizeBytes = await ReadExactAsync(4);
        var size = BitConverter.ToInt32(sizeBytes, 0);
        var responseBytes = await ReadExactAsync(size);
        var id = BitConverter.ToInt32(responseBytes, 0);
        var type = BitConverter.ToInt32(responseBytes, 4);
        var body = Encoding.UTF8.GetString(responseBytes, 8, size - 10); // remove id/type/nulls
        return (id, type, body);
    }

    private async Task<byte[]> ReadExactAsync(int length)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var read = await _stream!.ReadAsync(buffer, offset, length - offset);
            if (read == 0)
                ConsoleExt.WriteLine("Connection closed unexpectedly.", ConsoleExt.CurrentStep.RconQuery,
                    ConsoleExt.OutputType.Error, new IOException("Connection closed by remote host"));
            offset += read;
        }

        return buffer;
    }
}