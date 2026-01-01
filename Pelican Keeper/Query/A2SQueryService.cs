using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pelican_Keeper.Query;

/// <summary>
/// Queries Source engine servers using A2S protocol.
/// </summary>
public static class A2SQueryService
{
    private const byte A2SInfoHeader = 0x54;
    private const int Timeout = 3000;

    /// <summary>
    /// Queries a Source engine server for player count.
    /// </summary>
    public static async Task<string> QueryAsync(string ip, int port)
    {
        using var client = new UdpClient { Client = { ReceiveTimeout = Timeout, SendTimeout = Timeout } };

        try
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            client.Connect(endpoint);

            var request = BuildInfoRequest();
            await client.SendAsync(request);

            var response = await ReceiveWithChallengeAsync(client, endpoint);
            return ParseInfoResponse(response);
        }
        catch
        {
            return "N/A";
        }
    }

    private static byte[] BuildInfoRequest(int? challenge = null)
    {
        var query = "Source Engine Query\0"u8.ToArray();
        var packet = new byte[5 + query.Length + (challenge.HasValue ? 4 : 0)];

        packet[0] = 0xFF;
        packet[1] = 0xFF;
        packet[2] = 0xFF;
        packet[3] = 0xFF;
        packet[4] = A2SInfoHeader;
        query.CopyTo(packet, 5);

        if (challenge.HasValue)
            BitConverter.GetBytes(challenge.Value).CopyTo(packet, 5 + query.Length);

        return packet;
    }

    private static async Task<byte[]> ReceiveWithChallengeAsync(UdpClient client, IPEndPoint endpoint)
    {
        var result = await client.ReceiveAsync();
        var data = result.Buffer;

        if (data.Length >= 9 && data[4] == 0x41)
        {
            var challenge = BitConverter.ToInt32(data, 5);
            var newRequest = BuildInfoRequest(challenge);
            await client.SendAsync(newRequest);
            result = await client.ReceiveAsync();
            data = result.Buffer;
        }

        return data;
    }

    private static string ParseInfoResponse(byte[] data)
    {
        if (data.Length < 25) return "N/A";

        var pos = 6;

        pos = SkipString(data, pos); // Name
        pos = SkipString(data, pos); // Map
        pos = SkipString(data, pos); // Folder
        pos = SkipString(data, pos); // Game

        pos += 2; // Steam App ID

        if (pos + 2 > data.Length) return "N/A";

        var players = data[pos];
        var maxPlayers = data[pos + 1];

        return $"{players}/{maxPlayers}";
    }

    private static int SkipString(byte[] data, int pos)
    {
        while (pos < data.Length && data[pos] != 0) pos++;
        return pos + 1;
    }
}
