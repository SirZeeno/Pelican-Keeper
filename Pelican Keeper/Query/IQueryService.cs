namespace Pelican_Keeper.Query;

/// <summary>
/// Unified interface for game server queries.
/// </summary>
public interface IQueryService : IDisposable
{
    /// <summary>
    /// Gets or sets the server IP address.
    /// </summary>
    string Ip { get; set; }

    /// <summary>
    /// Gets or sets the server query port.
    /// </summary>
    int Port { get; set; }

    /// <summary>
    /// Connects to the server asynchronously.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Queries the server for player count or status.
    /// </summary>
    Task<string> QueryAsync();
}
