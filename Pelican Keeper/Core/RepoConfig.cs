namespace Pelican_Keeper.Core;

/// <summary>
/// Centralized repository configuration for update checking and default file downloads.
/// All values are configured via environment variables set in Pelican Panel.
/// </summary>
public static class RepoConfig
{
    /// <summary>
    /// Repository owner. Set via REPO_OWNER environment variable.
    /// </summary>
    public static string? Owner => Environment.GetEnvironmentVariable("REPO_OWNER");

    /// <summary>
    /// Repository name. Set via REPO_NAME environment variable.
    /// </summary>
    public static string? Repo => Environment.GetEnvironmentVariable("REPO_NAME");

    /// <summary>
    /// Branch name for raw content downloads. Set via REPO_BRANCH environment variable.
    /// </summary>
    public static string Branch => Environment.GetEnvironmentVariable("REPO_BRANCH") ?? "main";

    /// <summary>
    /// Generates a raw GitHub content URL for the specified file path.
    /// Returns null if REPO_OWNER or REPO_NAME are not configured.
    /// </summary>
    /// <param name="path">Relative path within the repository.</param>
    /// <returns>Full URL to the raw file content, or null if repo not configured.</returns>
    public static string? GetRawContentUrl(string path) =>
        Owner != null && Repo != null
            ? $"https://raw.githubusercontent.com/{Owner}/{Repo}/refs/heads/{Branch}/{path}"
            : null;

    /// <summary>
    /// GitHub API URL for fetching the latest release information.
    /// Returns null if REPO_OWNER or REPO_NAME are not configured.
    /// </summary>
    public static string? LatestReleaseApiUrl =>
        Owner != null && Repo != null
            ? $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest"
            : null;

    /// <summary>
    /// Gets the expected asset filename pattern for the current platform.
    /// </summary>
    public static string GetPlatformAssetPattern()
    {
        if (OperatingSystem.IsWindows())
            return Environment.Is64BitProcess ? "win-x64.zip" : "win-x86.zip";

        if (OperatingSystem.IsLinux())
        {
            var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
            return arch switch
            {
                System.Runtime.InteropServices.Architecture.Arm64 => "linux-arm64.zip",
                System.Runtime.InteropServices.Architecture.Arm => "linux-arm.zip",
                _ => "linux-x64.zip"
            };
        }

        if (OperatingSystem.IsMacOS())
        {
            var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
            return arch == System.Runtime.InteropServices.Architecture.Arm64 ? "osx-arm64.zip" : "osx-x64.zip";
        }

        return "linux-x64.zip";
    }
}
