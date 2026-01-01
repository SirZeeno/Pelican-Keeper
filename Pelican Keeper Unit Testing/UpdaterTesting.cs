using Pelican_Keeper;

namespace Pelican_Keeper_Unit_Testing;

public class UpdaterTesting
{
    [SetUp]
    public void Setup()
    {
        ConsoleExt.SuppressProcessExitForTests = true;
        VersionUpdater.CurrentVersion = "v2.0.2";
        ConsoleExt.WriteLineWithPretext($"Current version: {VersionUpdater.CurrentVersion}");
    }

    [Test]
    public async Task UpdateVersionTest()
    {
        ConsoleExt.WriteLineWithPretext("Starting Updater...");
        await VersionUpdater.UpdateProgram(); //TODO: Unable to test because the bot will exit itself when using the bash script
        ConsoleExt.WriteLineWithPretext("Updater Finished!\n");

        if (ConsoleExt.ExceptionOccurred)
            Assert.Fail($"Test failed due to exception(s): {ConsoleExt.Exceptions}\n");
        else if (VersionUpdater.CurrentVersion != "v2.0.2")
            Assert.Pass("Rcon command sent successfully.\n");
    }
}