using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Pelican_Keeper.Core;
using Pelican_Keeper.Utilities;
using RestSharp;
using DSharpPlus.Entities;

namespace Pelican_Keeper.Updates;

/// <summary>
/// Handles version checking, update notifications, and automatic updates.
/// </summary>
public static class VersionUpdater
{
    private static string? _latestVersion;
    private static string? _latestReleaseUrl;
    private static string? _releaseNotes;
    private static bool _updateAvailable;

    /// <summary>
    /// Gets whether an update is available.
    /// </summary>
    public static bool UpdateAvailable => _updateAvailable;

    /// <summary>
    /// Gets the latest version string.
    /// </summary>
    public static string? LatestVersion => _latestVersion;

    /// <summary>
    /// Checks for updates and handles notification/auto-update based on configuration.
    /// </summary>
    public static async Task CheckForUpdatesAsync()
    {
        await FetchLatestReleaseInfoAsync();

        if (!_updateAvailable)
        {
            Logger.WriteLineWithStep("Running latest version.", Logger.Step.Initialization);
            return;
        }

        Logger.WriteLineWithStep($"Update available: {RuntimeContext.Version} → {_latestVersion}", Logger.Step.Initialization, Logger.OutputType.Warning);

        // Notification is sent after Discord connects in Program.OnClientReady
        // Auto-update is also handled there to ensure channels are available
    }

    /// <summary>
    /// Checks if auto-update is enabled via environment variable.
    /// </summary>
    public static bool IsAutoUpdateEnabled()
    {
        var envValue = Environment.GetEnvironmentVariable("AUTO_UPDATE");
        return envValue?.ToLower() == "true" || envValue == "1";
    }

    /// <summary>
    /// Fetches the latest release information from GitHub API.
    /// </summary>
    public static async Task FetchLatestReleaseInfoAsync()
    {
        if (RepoConfig.LatestReleaseApiUrl == null)
        {
            Logger.WriteLineWithStep("REPO_OWNER/REPO_NAME not configured. Skipping update check.", Logger.Step.Initialization, Logger.OutputType.Warning);
            return;
        }

        try
        {
            var client = new RestClient(RepoConfig.LatestReleaseApiUrl);
            var request = new RestRequest("") { Timeout = TimeSpan.FromSeconds(10) };
            request.AddHeader("User-Agent", $"PelicanKeeper/{RuntimeContext.Version}");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                Logger.WriteLineWithStep("Failed to fetch release info.", Logger.Step.Initialization, Logger.OutputType.Warning);
                return;
            }

            using var doc = JsonDocument.Parse(response.Content);
            var root = doc.RootElement;

            _latestVersion = root.GetProperty("tag_name").GetString()?.TrimStart('v');
            _latestReleaseUrl = root.GetProperty("html_url").GetString();
            _releaseNotes = root.TryGetProperty("body", out var body) ? body.GetString() : null;

            if (_latestVersion != null && RuntimeContext.Version != null)
            {
                _updateAvailable = CompareVersions(RuntimeContext.Version, _latestVersion) < 0;
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLineWithStep($"Version check failed: {ex.Message}", Logger.Step.Initialization, Logger.OutputType.Warning);
        }
    }

    /// <summary>
    /// Compares two semantic version strings.
    /// </summary>
    private static int CompareVersions(string current, string latest)
    {
        var currentParts = current.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var latestParts = latest.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        for (var i = 0; i < Math.Max(currentParts.Length, latestParts.Length); i++)
        {
            var c = i < currentParts.Length ? currentParts[i] : 0;
            var l = i < latestParts.Length ? latestParts[i] : 0;

            if (c != l) return c.CompareTo(l);
        }

        return 0;
    }

    /// <summary>
    /// Sends an update notification to Discord.
    /// </summary>
    public static async Task SendDiscordNotificationAsync()
    {
        if (RuntimeContext.TargetChannels.Count == 0)
        {
            Logger.WriteLineWithStep("No channels available for update notification.", Logger.Step.Discord, Logger.OutputType.Warning);
            return;
        }

        var embed = new DiscordEmbedBuilder
        {
            Title = "🔄 Update Available",
            Description = $"A new version of Pelican Keeper is available!",
            Color = new DiscordColor(0x3498DB),
            Timestamp = DateTimeOffset.UtcNow
        };

        embed.AddField("Current Version", $"`{RuntimeContext.Version}`", true);
        embed.AddField("Latest Version", $"`{_latestVersion}`", true);

        if (!string.IsNullOrEmpty(_latestReleaseUrl))
        {
            embed.AddField("Release", $"[View on GitHub]({_latestReleaseUrl})", false);
        }

        if (!string.IsNullOrEmpty(_releaseNotes))
        {
            var truncatedNotes = _releaseNotes.Length > 500
                ? _releaseNotes[..500] + "..."
                : _releaseNotes;
            embed.AddField("Release Notes", truncatedNotes, false);
        }

        if (IsAutoUpdateEnabled())
        {
            embed.AddField("Auto-Update", "✅ Enabled - Update will be applied automatically", false);
        }
        else
        {
            embed.AddField("Auto-Update", "❌ Disabled - Set `AUTO_UPDATE=true` to enable", false);
        }

        embed.WithFooter($"{RepoConfig.Owner}/{RepoConfig.Repo}");

        try
        {
            foreach (var channel in RuntimeContext.TargetChannels)
            {
                await channel.SendMessageAsync(embed);
            }

            Logger.WriteLineWithStep("Update notification sent to Discord.", Logger.Step.Discord);
        }
        catch (Exception ex)
        {
            Logger.WriteLineWithStep($"Failed to send update notification: {ex.Message}", Logger.Step.Discord, Logger.OutputType.Error);
        }
    }

    /// <summary>
    /// Performs the automatic update process.
    /// </summary>
    public static async Task PerformUpdateAsync()
    {
        var assetPattern = RepoConfig.GetPlatformAssetPattern();
        var downloadUrl = await GetPlatformAssetUrlAsync(assetPattern);

        if (string.IsNullOrEmpty(downloadUrl))
        {
            Logger.WriteLineWithStep($"No matching release asset found for pattern: {assetPattern}", Logger.Step.Initialization, Logger.OutputType.Error);
            return;
        }

        Logger.WriteLineWithStep($"Downloading update from: {downloadUrl}", Logger.Step.Initialization);

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "pelican-keeper-update");
            Directory.CreateDirectory(tempPath);

            var archivePath = Path.Combine(tempPath, "update.zip");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", $"PelicanKeeper/{RuntimeContext.Version}");
                var data = await client.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(archivePath, data);
            }

            var scriptPath = Path.Combine(tempPath, "update.sh");
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var scriptContent = GenerateUpdateScript(archivePath, currentDir);

            await File.WriteAllTextAsync(scriptPath, scriptContent);

            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = scriptPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
            Logger.WriteLineWithStep("Update initiated. Application will restart.", Logger.Step.Initialization);

            await Task.Delay(1000);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Logger.WriteLineWithStep($"Update failed: {ex.Message}", Logger.Step.Initialization, Logger.OutputType.Error, ex);
        }
    }

    /// <summary>
    /// Gets the download URL for the platform-specific release asset.
    /// </summary>
    private static async Task<string?> GetPlatformAssetUrlAsync(string pattern)
    {
        if (RepoConfig.LatestReleaseApiUrl == null)
            return null;

        try
        {
            var client = new RestClient(RepoConfig.LatestReleaseApiUrl);
            var request = new RestRequest("") { Timeout = TimeSpan.FromSeconds(10) };
            request.AddHeader("User-Agent", $"PelicanKeeper/{RuntimeContext.Version}");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                return null;

            using var doc = JsonDocument.Parse(response.Content);
            var assets = doc.RootElement.GetProperty("assets");

            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                if (name != null && name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return asset.GetProperty("browser_download_url").GetString();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLineWithStep($"Failed to fetch asset URL: {ex.Message}", Logger.Step.Initialization, Logger.OutputType.Error);
        }

        return null;
    }

    /// <summary>
    /// Generates the update shell script.
    /// </summary>
    private static string GenerateUpdateScript(string archivePath, string targetDir)
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "Pelican Keeper.exe"
            : "Pelican Keeper";

        // For Docker/Pelican: extract update and exit. Pelican will restart the container.
        return $@"#!/bin/bash
sleep 1

cd ""{targetDir}""

# Extract zip (CI builds .zip files)
unzip -o ""{archivePath}"" -d ""{targetDir}""

chmod +x ""{Path.Combine(targetDir, executableName)}""

rm -rf ""{Path.GetDirectoryName(archivePath)}""

# Exit cleanly - Pelican will restart with updated binary
exit 0
";
    }

    /// <summary>
    /// Starts a background task to periodically check for updates.
    /// </summary>
    public static void StartPeriodicUpdateCheck(TimeSpan interval)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(interval);
                await CheckForUpdatesAsync();
            }
        });
    }
}
