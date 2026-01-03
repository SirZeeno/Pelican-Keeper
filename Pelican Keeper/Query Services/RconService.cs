using System.Net.Sockets;
using System.Text;

namespace Pelican_Keeper.Query_Services;

public class RconService(string ip, int port, string password) : ISendCommand, IDisposable
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private int _requestId;

    public readonly string Ip = ip;
    public readonly int Port = port;
    
    public async Task Connect()
    {
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(Ip, Port);
        _stream = _tcpClient.GetStream();
        
        bool authenticated = await AuthenticateAsync();
        if (authenticated && Program.Config.Debug)
            ConsoleExt.WriteLineWithStepPretext("RCON connection established successfully.", ConsoleExt.CurrentStep.RconQuery);
        else
            ConsoleExt.WriteLineWithStepPretext("RCON authentication failed.", ConsoleExt.CurrentStep.RconQuery, ConsoleExt.OutputType.Error, new UnauthorizedAccessException());
    }

    private async Task<bool> AuthenticateAsync()
    {
        _requestId++;
        byte[] packet = CreatePacket(_requestId, 3, password);
        await _stream!.WriteAsync(packet);

        var response = await ReadResponseAsync();
        return response.type == 2 && response.id == _requestId;
    }

    public async Task<string> SendCommandAsync(string command, string? regexPattern)
    {
        if (_tcpClient == null || _stream == null)
            throw new InvalidOperationException("Call Connect() before sending commands.");
        _requestId++;
        byte[] packet = CreatePacket(_requestId, 2, command);
        await _stream.WriteAsync(packet);

        var response = await ReadResponseAsync();
        if (Program.Config.Debug)
            ConsoleExt.WriteLineWithStepPretext($"RCON command response: {response.body.Trim()}", ConsoleExt.CurrentStep.RconQuery);
        return HelperClass.ExtractPlayerCount(response.body.Trim(), regexPattern).ToString();
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _tcpClient?.Close();
    }
    
    private byte[] CreatePacket(int id, int type, string body)   
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        byte[] packet = new byte[4 + 4 + bodyBytes.Length + 2]; // Size = Id + Type + Body + 2 null bytes
        
        BitConverter.GetBytes(id).CopyTo(packet, 0);
        BitConverter.GetBytes(type).CopyTo(packet, 4);
        bodyBytes.CopyTo(packet, 8);
        packet[^2] = 0; // Null terminator
        packet[^1] = 0; // Null terminator
        
        byte[] fullPacket = new byte[4 + packet.Length]; // Full packet size = Length + packet
        BitConverter.GetBytes(packet.Length).CopyTo(fullPacket, 0);
        packet.CopyTo(fullPacket, 4);
        
        return fullPacket;
    }
    
    private async Task<(int id, int type, string body)> ReadResponseAsync()
    {
        byte[] sizeBytes = await ReadExactAsync(4);
        int size = BitConverter.ToInt32(sizeBytes, 0);
        byte[] responseBytes = await ReadExactAsync(size);
        int id = BitConverter.ToInt32(responseBytes, 0);
        int type = BitConverter.ToInt32(responseBytes, 4);
        string body = Encoding.UTF8.GetString(responseBytes, 8, size - 10); // remove id/type/nulls
        return (id, type, body);
    }

    private async Task<byte[]> ReadExactAsync(int length)
    {
        byte[] buffer = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            int read = await _stream!.ReadAsync(buffer, offset, length - offset);
            if (read == 0) ConsoleExt.WriteLineWithStepPretext("Connection closed unexpectedly.", ConsoleExt.CurrentStep.RconQuery, ConsoleExt.OutputType.Error, new IOException("Connection closed by remote host"));
            offset += read;
        }
        return buffer;
    }
}