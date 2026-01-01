using System.Net.Sockets;
using System.Text;

namespace Pelican_Keeper.Query_Services;

public class JavaMinecraftQueryService : IDisposable
{
    private readonly string _ip;
    private readonly int _port;
    private readonly TcpClient _tcpClient;

    public JavaMinecraftQueryService(string ip, int port)
    {
        _ip = ip;
        _port = port;
        _tcpClient = new TcpClient();
    }

    public async Task Connect()
    {
        try
        {
            await _tcpClient.ConnectAsync(_ip, _port);
        }
        catch (SocketException)
        {
            // Ignore connection refused (Server is offline/restarting)
            // This prevents the [Error] log spam when stopping a server
        }
        catch (Exception ex)
        {
            if (Program.Config.Debug)
                ConsoleExt.WriteLineWithStepPretext($"Java Query Connect Error: {ex.Message}", ConsoleExt.CurrentStep.MinecraftJavaRequest, ConsoleExt.OutputType.Warning);
        }
    }

    public async Task<string> SendCommandAsync()
    {
        if (!_tcpClient.Connected) return "N/A";

        try
        {
            var stream = _tcpClient.GetStream();
            var payload = new List<byte> { 0x00 }; // Handshake packet ID
            payload.AddRange(GetVarInt(47)); // Protocol version
            payload.AddRange(Encoding.UTF8.GetBytes(_ip)); // Server address
            payload.AddRange(BitConverter.GetBytes((ushort)_port).Reverse()); // Server port
            payload.Add(0x01); // Next state: status

            var handshakePacket = new List<byte>();
            handshakePacket.AddRange(GetVarInt(payload.Count));
            handshakePacket.AddRange(payload);

            await stream.WriteAsync(handshakePacket.ToArray());

            // Status Request
            var statusRequest = new List<byte> { 0x01, 0x00 }; // Size 1, Packet ID 0
            await stream.WriteAsync(statusRequest.ToArray());

            // Read Response
            var buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0) return "N/A";

            string jsonResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // Basic cleaning of the JSON response to find player count "players":{"max":20,"online":0}
            // A simple regex is more robust than full JSON parsing for just this snippet in a raw stream
            var onlineMatch = System.Text.RegularExpressions.Regex.Match(jsonResponse, @"\""online\"":\s*(\d+)");
            var maxMatch = System.Text.RegularExpressions.Regex.Match(jsonResponse, @"\""max\"":\s*(\d+)");

            if (onlineMatch.Success && maxMatch.Success)
            {
                return $"{onlineMatch.Groups[1].Value}/{maxMatch.Groups[1].Value}";
            }

            return "N/A";
        }
        catch (Exception ex)
        {
            if (Program.Config.Debug)
                ConsoleExt.WriteLineWithStepPretext($"Java Query Error: {ex.Message}", ConsoleExt.CurrentStep.MinecraftJavaRequest);
            return "N/A";
        }
    }

    private byte[] GetVarInt(int value)
    {
        var bytes = new List<byte>();
        while ((value & 128) != 0)
        {
            bytes.Add((byte)(value & 127 | 128));
            value = (int)((uint)value >> 7);
        }
        bytes.Add((byte)value);
        return bytes.ToArray();
    }

    public void Dispose()
    {
        _tcpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}