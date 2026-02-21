using System.Globalization;

namespace Pelican_Keeper.Helper_Classes;

public static class ConversionHelpers
{
    /// <summary>
    /// Takes an input string number and checks if its zero, null or empty
    /// </summary>
    /// <param name="value">input string number</param>
    /// <returns>null if null of empty, or Infinite if 0, otherwise its Original value</returns>
    public static string IfZeroThenInfinite(string value)
    {
        return String.IsNullOrEmpty(value) ? "null" : value is "0" or "0.00" ? "∞" : value;
    }

    /// <summary>
    /// Adds a % at the end if it's a number
    /// </summary>
    /// <param name="value">Input String</param>
    /// <returns>Original Value if ∞ or the word null, or number with % at the end if a number</returns>
    public static string DynamicallyAddPercentSign(string value)
    { 
        return value is "∞" or "null"
            ? value
            : double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                ? $"{value}%"
                : value;
    }
    
    /// <summary>
    /// Cleans up the Server response into a clean and readable end user display string.
    /// </summary>
    /// <param name="serverResponse">The Server response</param>
    /// <param name="maxPlayers">Optional! A hard-coded string if left empty for the max number the server can have</param>
    /// <returns>A User readable string of the player count</returns>
    public static string ServerPlayerCountDisplayCleanup(string? serverResponse, int maxPlayers = 0)
    {
        string maxPlayerCount = "Unknown";
        
        if (string.IsNullOrEmpty(serverResponse) && maxPlayers > 0)
        {
            return $"N/A/{maxPlayers}";
        }
        
        if (string.IsNullOrEmpty(serverResponse))
        {
            return "N/A";
        }

        if (maxPlayers != 0)
        {
            maxPlayerCount = maxPlayers.ToString();
        }

        return $"{serverResponse}/{maxPlayerCount}";
    }
}