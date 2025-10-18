using System.Diagnostics;

namespace Pelican_Keeper;

public static class VersionUpdater
{
    private static void PreliminaryChecks()
    {
        if (!IsGitInstalled(out var gitVersion))
        {
            throw new InvalidOperationException("Git is not installed or not found in the system PATH. Please install Git to proceed with the update.");
        }

        ConsoleExt.WriteLineWithPretext($"Git is installed: {gitVersion}");
    }
    
    
    private static bool IsGitInstalled(out string? version)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            version = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            version = null;
            return false;
        }
    }
}