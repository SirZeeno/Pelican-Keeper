using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pelican_Keeper.Query;

/// <summary>
/// Queries Minecraft Bedrock Edition servers using RakNet protocol.
/// </summary>
public sealed class MinecraftBedrockQueryService : IQueryService
{
    private UdpClient? _client;
    private bool _disposed;

    /// <inheritdoc />
    public string Ip { get; set; }

    /// <inheritdoc />
    public int Port { get; set; }

    private const int Timeout = 5000;

    /// <summary>
    /// Initializes a new Minecraft Bedrock query service.
    /// </summary>
    public MinecraftBedrockQueryService(string ip, int port)
    {
        Ip = ip;
        Port = port;
    }

    /// <inheritdoc />
    public Task ConnectAsync()
    {
        _client = new UdpClient { Client = { ReceiveTimeout = Timeout, SendTimeout = Timeout } };
        _client.Connect(new IPEndPoint(IPAddress.Parse(Ip), Port));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync()
    {
        if (_client == null) return "N/A";

        try
        {
            var request = BuildUnconnectedPingPacket();
            await _client.SendAsync(request);

            var result = await _client.ReceiveAsync();
            return ParseUnconnectedPongPacket(result.Buffer);
        }
        catch
        {
            return "N/A";
        }
    }

    private static byte[] BuildUnconnectedPingPacket()
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x01); // Unconnected Ping

        // Timestamp (8 bytes)
        ms.Write(BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));

        // RakNet Magic (16 bytes)
        byte[] magic = [0x00, 0xFF, 0xFF, 0x00, 0xFE, 0xFE, 0xFE, 0xFE, 0xFD, 0xFD, 0xFD, 0xFD, 0x12, 0x34, 0x56, 0x78];
        ms.Write(magic);

        // Client GUID (8 bytes)
        ms.Write(BitConverter.GetBytes(Random.Shared.NextInt64()));

        return ms.ToArray();
    }

    private static string ParseUnconnectedPongPacket(byte[] data)
    {
        if (data.Length < 35 || data[0] != 0x1C) return "N/A";

        // Skip: ID (1) + Timestamp (8) + Server GUID (8) + Magic (16) + String length (2) = 35
        var stringLength = (data[33] << 8) | data[34];
        if (data.Length < 35 + stringLength) return "N/A";

        var serverInfo = Encoding.UTF8.GetString(data, 35, stringLength);
        var parts = serverInfo.Split(';');

        // Format: Edition;MOTD;ProtocolVersion;Version;PlayerCount;MaxPlayers;...
        if (parts.Length >= 6)
        {
            return $"{parts[4]}/{parts[5]}";
        }

        return "N/A";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _client?.Dispose();
        _disposed = true;
    }
}
