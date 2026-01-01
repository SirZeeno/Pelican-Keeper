using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Pelican_Keeper.Query;

/// <summary>
/// Queries game servers using RCON protocol.
/// </summary>
public sealed class RconQueryService : IQueryService
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly string _password;
    private int _packetId;
    private bool _isAuthenticated;
    private bool _disposed;

    /// <inheritdoc />
    public string Ip { get; set; }

    /// <inheritdoc />
    public int Port { get; set; }

    private const int AuthPacket = 3;
    private const int AuthResponse = 2;
    private const int ExecCommand = 2;

    /// <summary>
    /// Initializes a new RCON query service.
    /// </summary>
    public RconQueryService(string ip, int port, string password)
    {
        Ip = ip;
        Port = port;
        _password = password;
    }

    /// <inheritdoc />
    public async Task ConnectAsync()
    {
        if (_isAuthenticated && _client?.Connected == true) return;

        _client = new TcpClient { ReceiveTimeout = 5000, SendTimeout = 5000 };

        try
        {
            await _client.ConnectAsync(Ip, Port);
            _stream = _client.GetStream();
            _isAuthenticated = await AuthenticateAsync();
        }
        catch (Exception)
        {
            Cleanup();
        }
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync() => await QueryAsync("status", null);

    /// <summary>
    /// Executes an RCON command and optionally extracts data using regex.
    /// </summary>
    public async Task<string> QueryAsync(string command, string? extractRegex)
    {
        if (!_isAuthenticated || _stream == null) return "N/A";

        try
        {
            var packet = BuildPacket(ExecCommand, command);
            await _stream.WriteAsync(packet);

            var response = new StringBuilder();
            do
            {
                var buffer = new byte[4096];
                var length = await _stream.ReadAsync(buffer);
                if (length < 12) break;
                response.Append(Encoding.UTF8.GetString(buffer, 12, length - 14));
            } while (_stream.DataAvailable);

            var responseText = response.ToString();
            if (extractRegex != null)
            {
                var match = Regex.Match(responseText, extractRegex);
                return match.Success ? match.Groups[1].Value : "N/A";
            }

            return responseText;
        }
        catch
        {
            return "N/A";
        }
    }

    private async Task<bool> AuthenticateAsync()
    {
        if (_stream == null) return false;

        try
        {
            var authPacket = BuildPacket(AuthPacket, _password);
            await _stream.WriteAsync(authPacket);

            var buffer = new byte[4096];
            await _stream.ReadAsync(buffer);

            var responseId = BitConverter.ToInt32(buffer, 4);
            return responseId != -1;
        }
        catch
        {
            return false;
        }
    }

    private byte[] BuildPacket(int type, string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var packetSize = 10 + bodyBytes.Length;
        var packet = new byte[4 + packetSize];

        BitConverter.GetBytes(packetSize).CopyTo(packet, 0);
        BitConverter.GetBytes(++_packetId).CopyTo(packet, 4);
        BitConverter.GetBytes(type).CopyTo(packet, 8);
        bodyBytes.CopyTo(packet, 12);

        return packet;
    }

    private void Cleanup()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        _isAuthenticated = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        Cleanup();
        _disposed = true;
    }
}
