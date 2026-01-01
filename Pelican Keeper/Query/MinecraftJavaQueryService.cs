using System.Net.Sockets;
using System.Text;

namespace Pelican_Keeper.Query;

/// <summary>
/// Queries Minecraft Java Edition servers using SLP protocol.
/// </summary>
public sealed class MinecraftJavaQueryService : IQueryService
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _disposed;

    /// <inheritdoc />
    public string Ip { get; set; }

    /// <inheritdoc />
    public int Port { get; set; }

    private const int Timeout = 5000;

    /// <summary>
    /// Initializes a new Minecraft Java query service.
    /// </summary>
    public MinecraftJavaQueryService(string ip, int port)
    {
        Ip = ip;
        Port = port;
    }

    /// <inheritdoc />
    public async Task ConnectAsync()
    {
        _client = new TcpClient { ReceiveTimeout = Timeout, SendTimeout = Timeout };
        await _client.ConnectAsync(Ip, Port);
        _stream = _client.GetStream();
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync()
    {
        if (_stream == null) return "N/A";

        try
        {
            await SendHandshakeAsync();
            await SendStatusRequestAsync();

            var response = await ReadResponseAsync();
            return ParsePlayerCount(response);
        }
        catch
        {
            return "N/A";
        }
    }

    private async Task SendHandshakeAsync()
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x00); // Packet ID

        WriteVarInt(ms, 754); // Protocol version (1.16.5+)
        WriteString(ms, Ip);
        ms.Write(BitConverter.GetBytes((ushort)Port).Reverse().ToArray());
        WriteVarInt(ms, 1); // Next state: Status

        var payload = ms.ToArray();
        var packet = new MemoryStream();
        WriteVarInt(packet, payload.Length);
        packet.Write(payload);

        await _stream!.WriteAsync(packet.ToArray());
    }

    private async Task SendStatusRequestAsync()
    {
        var packet = new byte[] { 0x01, 0x00 }; // Length = 1, Packet ID = 0
        await _stream!.WriteAsync(packet);
    }

    private async Task<string> ReadResponseAsync()
    {
        ReadVarInt(_stream!); // Packet length
        ReadVarInt(_stream!); // Packet ID

        var stringLength = ReadVarInt(_stream!);
        var buffer = new byte[stringLength];
        await _stream!.ReadExactlyAsync(buffer);

        return Encoding.UTF8.GetString(buffer);
    }

    private static string ParsePlayerCount(string json)
    {
        var playersMatch = System.Text.RegularExpressions.Regex.Match(json, @"""players"":\{[^}]*""online"":(\d+)[^}]*""max"":(\d+)");
        if (!playersMatch.Success)
        {
            playersMatch = System.Text.RegularExpressions.Regex.Match(json, @"""online"":(\d+).*?""max"":(\d+)");
        }

        if (playersMatch.Success)
        {
            return $"{playersMatch.Groups[1].Value}/{playersMatch.Groups[2].Value}";
        }

        return "N/A";
    }

    private static void WriteVarInt(Stream stream, int value)
    {
        while ((value & ~0x7F) != 0)
        {
            stream.WriteByte((byte)((value & 0x7F) | 0x80));
            value = (int)((uint)value >> 7);
        }
        stream.WriteByte((byte)value);
    }

    private static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteVarInt(stream, bytes.Length);
        stream.Write(bytes);
    }

    private static int ReadVarInt(Stream stream)
    {
        var value = 0;
        var position = 0;
        byte currentByte;

        do
        {
            currentByte = (byte)stream.ReadByte();
            value |= (currentByte & 0x7F) << position;
            position += 7;

            if (position >= 32) throw new InvalidDataException("VarInt too long");
        } while ((currentByte & 0x80) != 0);

        return value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _stream?.Dispose();
        _client?.Dispose();
        _disposed = true;
    }
}
