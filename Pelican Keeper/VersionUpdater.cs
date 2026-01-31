using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pelican_Keeper;

public static class VersionUpdater
{
    public static string CurrentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "2.0.4";
    private static HttpClient _http = null!;
    
    public static async Task UpdateProgram()
    {
        ConsoleExt.WriteLine("Checking for updates...");
        SetupUserAgent();
        
        string? targetPath = await DownloadLatestAssetIfNewerAsync(CurrentVersion); // Downloads the appropriate version of the latest update for your platform if there is one available.
        if (string.IsNullOrEmpty(targetPath))
        {
            ConsoleExt.WriteLine("No update to apply.", ConsoleExt.CurrentStep.Updater);
            if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, "old")))
            {
                await ReadOldConfigs();
            }
            return;
        }
        
        ConsoleExt.WriteLine($"Download Completed! Path: {targetPath}", ConsoleExt.CurrentStep.Updater);
        BackupOldConfigs();
        ConsoleExt.WriteLine("Configs Backed up!", ConsoleExt.CurrentStep.Updater);
        
        string scriptPath = Path.Combine(Environment.CurrentDirectory, "update.sh");

        if (!File.Exists(scriptPath))
        {
            ConsoleExt.WriteLine($"Updater script not found at {scriptPath}", ConsoleExt.CurrentStep.Updater);
            return;
        }

        // Making sure it is executable on Linux (just in case)
        try
        {
            //await Process.Start("chmod", $"+x \"{scriptPath}\"").WaitForExitAsync();
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // rwxr-xr-x = 755
                File.SetUnixFileMode(scriptPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }
        catch
        {
            // just going to ignore if this doesn't work, because this only needs to get done on linux
        }

        int currentPid = Environment.ProcessId;
        var appName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Pelican-Keeper.exe" : "Pelican-Keeper"; // no .exe on Linux or macOS, so I need to dynamically change the RID

        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"\"{scriptPath}\" \"{targetPath}\" \"{Environment.CurrentDirectory}\" {currentPid} \"{appName}\"",
            UseShellExecute = false
        };

        ConsoleExt.WriteLine("Starting bash updater and exiting current process...", ConsoleExt.CurrentStep.Updater);
        Process.Start(psi);

        // Exit so the script can overwrite the running binary
        Environment.Exit(0);
    }

    private static void BackupOldConfigs()
    {
        string oldDirectory = Path.Combine(Environment.CurrentDirectory, "old");
        
        if (!Directory.Exists(oldDirectory))
        {
            Directory.CreateDirectory(oldDirectory);
        }
        
        DirectoryInfo directoryInfo = new(Environment.CurrentDirectory);
        FileInfo[] files = directoryInfo.GetFiles();

        foreach (var file in files)
        {
            if (!file.Name.EndsWith(".json")) continue;
            file.MoveTo(Path.Combine(oldDirectory,  file.Name), true);
            ConsoleExt.WriteLine($"Moving {file.Name} to {Path.Combine(oldDirectory, file.Name)}. If the File already exists in the Directory, it will be overwritten.", ConsoleExt.CurrentStep.Updater);
        }
    }

    private static void SetupUserAgent()
    {
        _http = new HttpClient();
        
        // GitHub requires a User-Agent header to download and access the releases according to what I read, so just in case I am creating a User-Agent.
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("PelicanKeeper-Updater/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    private record GitHubAsset(string Name, [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
    private record GitHubRelease(string TagName, string Name, GitHubAsset[] Assets);

    private static async Task<string?> DownloadLatestAssetIfNewerAsync(string currentVersionString)
    {
        var (hasUpdate, latestTag, latestVersion, release) = await CheckForUpdateAsync(currentVersionString);

        if (!hasUpdate)
        {
            ConsoleExt.WriteLine($"No update. Current={currentVersionString}, latest={latestTag}", ConsoleExt.CurrentStep.Updater);
            return null;
        }
        
        ConsoleExt.WriteLine($"Update available! Current={currentVersionString}, latest={latestTag}", ConsoleExt.CurrentStep.Updater);

        if (release?.Assets == null || release.Assets.Length == 0)
        {
            ConsoleExt.WriteLine("No assets on latest release.", ConsoleExt.CurrentStep.Updater, ConsoleExt.OutputType.Error);
            return null;
        }

        var rid = GetCurrentRid();
        var asset = Array.Find(release.Assets, a => a.Name.EndsWith($"{rid}.zip", StringComparison.OrdinalIgnoreCase)); // File format: Pelican-Keeper-vX.Y.Z-{rid}.zip

        if (asset == null)
        {
            ConsoleExt.WriteLine($"No asset found for RID '{rid}'.", ConsoleExt.CurrentStep.Updater, ConsoleExt.OutputType.Error);
            return null;
        }

        ConsoleExt.WriteLine($"Downloading {asset.Name} from {asset.BrowserDownloadUrl}", ConsoleExt.CurrentStep.Updater);
        
        var targetPath = Path.Combine(Environment.CurrentDirectory, asset.Name);

        using var resp = await _http.GetAsync(asset.BrowserDownloadUrl);
        resp.EnsureSuccessStatusCode();

        await using var s  = await resp.Content.ReadAsStreamAsync();
        await using var fs = File.Create(targetPath);
        await s.CopyToAsync(fs);

        return targetPath; // path to the downloaded zip
    }
    
    private static async Task<(bool hasUpdate, string latestTag, Version? latestVersion, GitHubRelease? release)> CheckForUpdateAsync(string currentVersionString)
    {
        // 1. Get latest release JSON
        var url = "https://api.github.com/repos/SirZeeno/Pelican-Keeper/releases/latest";
        using var resp = await _http.GetAsync(url);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync();
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (release == null)
            return (false, "", null, null);

        var latestTag = release.TagName; // e.g. "v2.0.3"

        // 2. Parse both versions
        var latestVersion  = TryParseVersion(latestTag);
        var currentVersion = TryParseVersion(currentVersionString);

        // If parsing fails, it falls back to simple "tag != current" style check
        if (latestVersion == null || currentVersion == null)
        {
            bool different = !string.Equals(latestTag, currentVersionString, StringComparison.OrdinalIgnoreCase);
            return (different, latestTag, latestVersion, release);
        }

        // 3. Compare
        bool hasUpdate = latestVersion > currentVersion;
        return (hasUpdate, latestTag, latestVersion, release);
    }

    private static string GetCurrentRid()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X86   => "win-x86",
                Architecture.X64   => "win-x64",
                Architecture.Arm64 => "win-arm64", // if this ever gets released from microsoft, it's implemented,
                _                  => "win-x64"
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64   => "linux-x64",
                Architecture.Arm   => "linux-arm",
                Architecture.Arm64 => "linux-arm64",
                _                  => "linux-x64"
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => "osx-arm64",
                _                  => "osx-x64"
            };
        }

        // fallback
        return "linux-x64";
    }

    private static Version? TryParseVersion(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // strip leading 'v' or 'V'
        if (input.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            input = input[1..];

        // cut off any suffix like "-beta", "+build", etc.
        var dashIndex = input.IndexOfAny(['-', '+']);
        if (dashIndex >= 0)
            input = input[..dashIndex];

        return Version.TryParse(input, out var v) ? v : null;
    }

    private static async Task ReadOldConfigs()
    {
        TemplateClasses.Secrets? oldSecrets = await FileManager.ReadSecretsFile(Path.Combine(Environment.CurrentDirectory, "old"));
        TemplateClasses.Config? oldConfig = await FileManager.ReadConfigFile(Path.Combine(Environment.CurrentDirectory, "old"));

        await FileManager.ReadSecretsFile(); // Creating the Default Secrets file
        
        await File.WriteAllTextAsync(Path.Combine(Environment.CurrentDirectory, "Secrets.json"), JsonSerializer.Serialize(oldSecrets, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
        
        await File.WriteAllTextAsync(Path.Combine(Environment.CurrentDirectory, "Config.json"), JsonSerializer.Serialize(oldConfig, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}