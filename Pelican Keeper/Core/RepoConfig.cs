namespace Pelican_Keeper.Core;

/// <summary>
/// Centralized repository configuration for update checking and default file downloads.
/// Supports environment variable overrides for flexibility in different deployment environments.
/// </summary>
public static class RepoConfig
{
    private const string DefaultOwner = "SirZeeno";
    private const string DefaultRepo = "Pelican-Keeper";
    private const string DefaultBranch = "main";

    /// <summary>
    /// Repository owner. Override via REPO_OWNER environment variable.
    /// </summary>
    public static string Owner => Environment.GetEnvironmentVariable("REPO_OWNER") ?? DefaultOwner;

    /// <summary>
    /// Repository name. Override via REPO_NAME environment variable.
    /// </summary>
    public static string Repo => Environment.GetEnvironmentVariable("REPO_NAME") ?? DefaultRepo;

    /// <summary>
    /// Branch name for raw content downloads. Override via REPO_BRANCH environment variable.
    /// </summary>
    public static string Branch => Environment.GetEnvironmentVariable("REPO_BRANCH") ?? DefaultBranch;

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
