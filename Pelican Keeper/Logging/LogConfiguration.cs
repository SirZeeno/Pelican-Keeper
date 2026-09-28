using Serilog;
namespace Pelican_Keeper.Logging;

public static class LogConfiguration
{
    public static void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                "Logs/pelican-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                flushToDiskInterval: TimeSpan.Zero,
                shared: false,
                outputTemplate: "{Timestamp:MM/dd/yyyy HH:mm:ss} [{Level:u3}] [{Step}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    public static void Close()
    {
        Log.CloseAndFlush();
    }
}