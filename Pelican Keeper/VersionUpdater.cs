using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using RestSharp;

namespace Pelican_Keeper;

public static class VersionUpdater
{
    // Updated to your repository
    private const string RepoOwner = "sersium";
    private const string RepoName = "pelican-keeper";

    public static async Task UpdateProgram()
    {
        ConsoleExt.WriteLineWithPretext("Checking for updates...");

        var client = new RestClient($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");
        var request = new RestRequest("");
        request.AddHeader("User-Agent", "Pelican-Keeper-Updater");

        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
        {
            ConsoleExt.WriteLineWithPretext("Failed to fetch update info.", ConsoleExt.OutputType.Error);
            return;
        }

        using var doc = JsonDocument.Parse(response.Content);
        var root = doc.RootElement;

        string tagName = root.GetProperty("tag_name").GetString() ?? "";

        string downloadUrl = "";
        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith("linux-x64.zip"))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(downloadUrl))
        {
            ConsoleExt.WriteLineWithPretext("No compatible update asset found.", ConsoleExt.OutputType.Warning);
            return;
        }

        ConsoleExt.WriteLineWithPretext($"Downloading update {tagName}...");

        using var httpClient = new HttpClient();
        var zipBytes = await httpClient.GetByteArrayAsync(downloadUrl);
        await File.WriteAllBytesAsync("update.zip", zipBytes);

        ConsoleExt.WriteLineWithPretext("Extracting update...");
        try
        {
            ZipFile.ExtractToDirectory("update.zip", "./", true);
            File.Delete("update.zip");
            Process.Start("chmod", "+x \"Pelican Keeper\"");
            ConsoleExt.WriteLineWithPretext("Update complete! Restarting...");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            ConsoleExt.WriteLineWithPretext($"Update failed: {ex.Message}", ConsoleExt.OutputType.Error);
        }
    }
}