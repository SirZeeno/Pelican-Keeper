namespace Pelican_Keeper.Core;

/// <summary>
/// Centralized repository configuration for update checking and default file downloads.
/// </summary>
public static class RepoConfig
{
    /// <summary>
    /// Repository owner.
    /// </summary>
    public static string Owner => "SirZeeno";

    /// <summary>
    /// Repository name.
    /// </summary>
    public static string Repo => "Pelican-Keeper";

    /// <summary>
    /// Branch name for raw content downloads.
    /// </summary>
    public static string Branch => "main";

    /// <summary>
    /// Generates a raw GitHub content URL for the specified file path.
    /// </summary>
    /// <param name="path">Relative path within the repository.</param>
    /// <returns>Full URL to the raw file content.</returns>
    public static string GetRawContentUrl(string path) =>
        $"https://raw.githubusercontent.com/{Owner}/{Repo}/refs/heads/{Branch}/{path}";

    /// <summary>
    /// GitHub API URL for fetching the latest release information.
    /// </summary>
    public static string LatestReleaseApiUrl =>
        $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";

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
