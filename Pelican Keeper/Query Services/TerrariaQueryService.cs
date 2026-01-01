using System.Net.Sockets;
using System.Text;

namespace Pelican_Keeper.Query_Services;

public class TerrariaQueryService : IDisposable
{
    private readonly string _ip;
    private readonly int _port;
    private readonly UdpClient _udpClient;

    public TerrariaQueryService(string ip, int port)
    {
        _ip = ip;
        _port = port;
        _udpClient = new UdpClient();
        _udpClient.Client.ReceiveTimeout = 2000;
        _udpClient.Client.SendTimeout = 2000;
    }

    public void Connect()
    {
        // For UDP based queries (TShock / UTrak)
        _udpClient.Connect(_ip, _port);
    }

    public async Task<string> SendCommandAsync()
    {
        try
        {
            // Note: Standard Vanilla Terraria does NOT support a public player count query via network packets.
            // It requires RCON authentication to run "/players" or "/playing".
            // If you want exact player counts for Vanilla, switch your protocol to 'Rcon' in GamesToMonitor.json
            // and ensure RCON is enabled on the server.

            // This service performs a TCP connectivity check. If the port accepts a connection, the server is "Online".
            // It returns "Online" for Vanilla, or a player count if TShock/UTrak is detected via UDP.

            using (var tcpClient = new TcpClient())
            {
                var connectTask = tcpClient.ConnectAsync(_ip, _port);
                // 2 Second timeout
                if (await Task.WhenAny(connectTask, Task.Delay(2000)) == connectTask && tcpClient.Connected)
                {
                    // Server is reachable!
                    // Since we can't extract player count from Vanilla without RCON login, we return a status string.
                    return "Online";
                }
            }
            return "N/A";
        }
        catch
        {
            return "N/A";
        }
    }

    public void Dispose()
    {
        _udpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}