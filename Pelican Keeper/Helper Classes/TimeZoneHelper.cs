using Pelican_Keeper.Logging;

namespace Pelican_Keeper.Helper_Classes;

public static class TimeZoneHelper
{
    // common time zone abbreviations to IANA / Windows IDs.
    // Some abbreviations are inherently ambiguous like 'CST'. Will try to get those resolved in the future.

    private static readonly Dictionary<string, string> AbbreviationToTimeZoneIds = new(StringComparer.OrdinalIgnoreCase)
    {
        { "GMT", "Etc/GMT" },
        { "UTC", "Etc/UTC" },
        { "ECT", "Europe/Paris" },
        { "EET", "Europe/Bucharest" },
        { "ART", "Africa/Cairo" },
        { "EAT", "Africa/Nairobi" },
        { "MET", "Asia/Tehran" },
        { "NET", "Asia/Yerevan" },
        { "PLT", "Asia/Karachi" },
        { "IST", "Asia/Kolkata" },
        { "BST", "Asia/Dhaka" },
        { "VST", "Asia/Ho_Chi_Minh" },
        { "CTT", "Asia/Shanghai" },
        { "JST", "Asia/Tokyo" },
        { "ACT", "Australia/Darwin" },
        { "AET", "Australia/Sydney" },
        { "SST", "Pacific/Guadalcanal" },
        { "NST", "Pacific/Auckland" },
        { "MIT", "Pacific/Apia" },
        { "HST", "Pacific/Honolulu" },
        { "AST", "America/Anchorage" },
        { "PST", "America/Los_Angeles" },
        { "PDT", "America/Los_Angeles" },
        { "MST", "America/Denver" },
        { "MDT", "America/Denver" },
        { "CST", "America/Chicago" },
        { "CDT", "America/Chicago" },
        { "EST", "America/New_York" },
        { "EDT", "America/New_York" },
        { "IET", "America/Indiana/Indianapolis" },
        { "PRT", "America/Puerto_Rico" },
        { "CNT", "America/St_Johns" },
        { "AGT", "America/Argentina/Buenos_Aires" },
        { "BET", "America/Sao_Paulo" },
        { "CAT", "Africa/Harare" },
        { "CET", "Europe/Berlin" },
        { "CEST", "Europe/Berlin" },
        { "WET", "Europe/Lisbon" },
        { "WEST", "Europe/Lisbon" },
        { "MSK", "Europe/Moscow" },
        { "AEST", "Australia/Brisbane" },
        { "AEDT", "Australia/Sydney" },
        { "AWST", "Australia/Perth" },
        { "ACST", "Australia/Adelaide" },
        { "ACDT", "Australia/Adelaide" }
    };

    /// <summary>
    /// Attempts to find a cross-platform TimeZoneInfo object using a time zone abbreviation.
    /// </summary>
    /// <param name="abbreviation"></param>
    /// <returns></returns>
    public static string TryFindSystemTimeZoneByAbbreviation(string abbreviation)
    {
        if (!AbbreviationToTimeZoneIds.TryGetValue(abbreviation, out var targetId))
        {
            ConsoleExt.WriteLine($"Couldn't find timezone id: {abbreviation}", ConsoleExt.CurrentStep.Helper, ConsoleExt.OutputType.Error);
            return string.Empty;
        }
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(targetId).Id;
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback for old non-ICU Windows setups
        }

        return string.Empty;
    }
}