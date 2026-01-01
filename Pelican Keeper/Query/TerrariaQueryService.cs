using System.Net;
using System.Net.Sockets;

namespace Pelican_Keeper.Query;

/// <summary>
/// Queries Terraria servers for availability status.
/// </summary>
public sealed class TerrariaQueryService : IQueryService
{
    private TcpClient? _client;
    private bool _disposed;

    /// <inheritdoc />
    public string Ip { get; set; }

    /// <inheritdoc />
    public int Port { get; set; }

    private const int Timeout = 5000;

    /// <summary>
    /// Initializes a new Terraria query service.
    /// </summary>
    public TerrariaQueryService(string ip, int port)
    {
        Ip = ip;
        Port = port;
    }

    /// <inheritdoc />
    public Task ConnectAsync()
    {
        _client = new TcpClient { ReceiveTimeout = Timeout, SendTimeout = Timeout };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync()
    {
        try
        {
            _client ??= new TcpClient { ReceiveTimeout = Timeout, SendTimeout = Timeout };

            using var cts = new CancellationTokenSource(Timeout);
            await _client.ConnectAsync(Ip, Port, cts.Token);

            return _client.Connected ? "Online" : "Offline";
        }
        catch
        {
            return "Offline";
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _client?.Dispose();
        _disposed = true;
    }
}
