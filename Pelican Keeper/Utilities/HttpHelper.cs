namespace Pelican_Keeper.Utilities;

/// <summary>
/// HTTP helper for fetching remote content.
/// </summary>
public static class HttpHelper
{
    private static readonly HttpClient Client = new();

    /// <summary>
    /// Fetches string content from a URL.
    /// </summary>
    public static async Task<string> GetStringAsync(string url)
    {
        return await Client.GetStringAsync(url);
    }

    /// <summary>
    /// Fetches byte array from a URL.
    /// </summary>
    public static async Task<byte[]> GetBytesAsync(string url)
    {
        return await Client.GetByteArrayAsync(url);
    }
}
