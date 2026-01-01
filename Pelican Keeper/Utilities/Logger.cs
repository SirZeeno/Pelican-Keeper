using System.Collections;

namespace Pelican_Keeper.Utilities;

/// <summary>
/// Centralized logging with timestamped, categorized console output.
/// </summary>
public static class Logger
{
    /// <summary>Exception tracking for diagnostics.</summary>
    public static bool ExceptionOccurred { get; private set; }
    private static readonly LinkedList<Exception> ExceptionList = new();
    public static IReadOnlyCollection<Exception> Exceptions => ExceptionList;

    /// <summary>Prevents process exit during unit tests.</summary>
    public static bool SuppressExitForTests { get; set; }

    public enum OutputType { Error, Info, Warning, Question }

    public enum Step
    {
        FileReading,
        MessageHistory,
        PelicanApi,
        A2SQuery,
        RconQuery,
        MinecraftJavaQuery,
        MinecraftBedrockQuery,
        DiscordMessage,
        DiscordInteraction,
        Discord,
        Updater,
        Markdown,
        Helper,
        GameMonitoring,
        EmbedBuilding,
        Initialization,
        None
    }

    /// <summary>
    /// Writes a log line with timestamp and output type.
    /// </summary>
    public static void WriteLine<T>(T message, OutputType type = OutputType.Info, Exception? ex = null, bool shouldExit = false)
    {
        WriteTimestamp();
        WriteOutputType(type);
        WriteMessage(message);
        HandleException(ex, shouldExit);
    }

    /// <summary>
    /// Writes a log line with timestamp, step context, and output type.
    /// </summary>
    public static void WriteLineWithStep<T>(T message, Step step = Step.None, OutputType type = OutputType.Info, Exception? ex = null, bool shouldExit = false)
    {
        WriteTimestamp();
        WriteStep(step);
        WriteOutputType(type);
        WriteMessage(message);
        HandleException(ex, shouldExit);
    }

    private static void WriteTimestamp()
    {
        var timestamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"[{timestamp}] ");
        Console.ResetColor();
    }

    private static void WriteOutputType(OutputType type)
    {
        var (color, label) = type switch
        {
            OutputType.Error => (ConsoleColor.DarkRed, "Error"),
            OutputType.Warning => (ConsoleColor.DarkYellow, "Warning"),
            OutputType.Question => (ConsoleColor.DarkGreen, "Question"),
            _ => (ConsoleColor.Green, "Info")
        };

        Console.ForegroundColor = color;
        Console.Write($"[{label}] ");
        Console.ResetColor();
    }

    private static void WriteStep(Step step)
    {
        if (step == Step.None) return;

        var label = step switch
        {
            Step.FileReading => "File Reading",
            Step.MessageHistory => "Message History",
            Step.PelicanApi => "Pelican API",
            Step.A2SQuery => "A2S Query",
            Step.RconQuery => "RCON Query",
            Step.MinecraftJavaQuery => "MC Java Query",
            Step.MinecraftBedrockQuery => "MC Bedrock Query",
            Step.DiscordMessage => "Discord Message",
            Step.DiscordInteraction => "Discord Interaction",
            Step.Discord => "Discord",
            Step.Updater => "Updater",
            Step.Markdown => "Markdown",
            Step.Helper => "Helper",
            Step.GameMonitoring => "Game Monitoring",
            Step.EmbedBuilding => "Embed Building",
            Step.Initialization => "Initialization",
            _ => step.ToString()
        };

        Console.Write($"[{label}] ");
    }

    private static void WriteMessage<T>(T message)
    {
        if (message is IEnumerable enumerable and not string)
            Console.Write(string.Join(", ", enumerable.Cast<object>()));
        else
            Console.Write(message);
    }

    private static void HandleException(Exception? ex, bool shouldExit)
    {
        Console.WriteLine();

        if (ex != null)
        {
            ExceptionOccurred = true;
            ExceptionList.AddLast(ex);
            Console.WriteLine($"Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
        }

        if (!shouldExit || SuppressExitForTests) return;

        Thread.Sleep(TimeSpan.FromSeconds(5));
        Environment.Exit(1);
    }

    /// <summary>Clears exception tracking state.</summary>
    public static void ClearExceptions()
    {
        ExceptionOccurred = false;
        ExceptionList.Clear();
    }
}
