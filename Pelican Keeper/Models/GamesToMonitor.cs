namespace Pelican_Keeper.Models;

/// <summary>
/// Configuration for game-specific player count queries.
/// </summary>
public class GamesToMonitor
{
    /// <summary>Egg/game name to match.</summary>
    public string Game { get; init; } = null!;

    /// <summary>Query protocol to use.</summary>
    public CommandExecutionMethod Protocol { get; init; }

    /// <summary>Environment variable name for RCON port.</summary>
    public string? RconPortVariable { get; set; }

    /// <summary>Environment variable name for RCON password.</summary>
    public string? RconPasswordVariable { get; set; }

    /// <summary>Static RCON password (alternative to variable).</summary>
    public string? RconPassword { get; set; }

    /// <summary>Command to send for player count.</summary>
    public string? Command { get; set; }

    /// <summary>Environment variable name for query port.</summary>
    public string? QueryPortVariable { get; set; }

    /// <summary>Environment variable name for max players.</summary>
    public string? MaxPlayerVariable { get; set; }

    /// <summary>Static max player count (alternative to variable).</summary>
    public string? MaxPlayer { get; set; }

    /// <summary>Custom regex for extracting player count from response.</summary>
    public string? PlayerCountExtractRegex { get; set; }
}
