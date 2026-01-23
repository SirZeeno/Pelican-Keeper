using System.Collections;

namespace Pelican_Keeper;

/// <summary>
/// TODOs
///
/// Work on optimizing the console output to be less laggy on slower systems due to the constant write outputs even per message
/// Move the config debug mode check into here so i only have to call the write line function and not also check if debug is on, before the call
/// Add the OutputMode enum into here to allow for granular control of what type of output is being displayed
/// Re-Organize the output types of all the console write calls to better reflect the new Debug Output type
/// </summary>
public static class ConsoleExt
{
    public static bool ExceptionOccurred;
    public static IReadOnlyCollection<Exception> Exceptions => ExceptionsList;
    private static readonly LinkedList<Exception> ExceptionsList = new();
    
    // For Unit testing, to stop the program from exiting during errors and causing no readable error or exception message
    public static bool SuppressProcessExitForTests { get; set; }
    
    public enum OutputType
    {
        Error,
        Info,
        Warning,
        Debug
    }
    
    // This is changeable to be whatever necessary
    public enum CurrentStep
    {
        FileChecks,
        MessageHistory,
        PelicanApi,
        A2SQuery,
        RconQuery,
        MinecraftJavaQuery,
        MinecraftBedrockQuery,
        DiscordMessage,
        DiscordInteraction,
        Updater,
        Markdown,
        Helper,
        GameMonitoring,
        EmbedBuilding,
        FileReading,
        Initialization,
        None
    }

    /// <summary>
    /// Writes a line to the console with a pretext based on the output type.
    /// </summary>
    /// <param name="output">Output</param>
    /// <param name="outputType">Output type, default is info</param>
    /// <param name="exception">Exception, default is null</param>
    /// <param name="shouldBypassDebug">Should the console Log Bypass Debug Mode</param>
    /// <param name="shouldExit">Should the Program Exit</param>
    /// <typeparam name="T">Any type</typeparam>
    /// <returns>The length of the pretext</returns>
    public static void WriteLineWithPretext<T>(T output, OutputType outputType = OutputType.Info, Exception? exception = null, bool shouldBypassDebug = false, bool shouldExit = false)
    {
        // It needs to write if the output is info
        // It needs to write if the output is an error
        // It needs to write if the bypass is true
        // It shouldn't write it the output is not info or error and the debug is off
        // It should only allow the output of the type that corresponds to the output mode if debug is on and output mode is set to anything but none
        if (!Program.Config.Debug && outputType != OutputType.Info && !shouldBypassDebug) return;
        
        CurrentTime();
        WriteOutputType(outputType);
        if (output is IEnumerable enumerable && !(output is string))
        {
            Console.Write(string.Join(", ", enumerable.Cast<object>()));
        }
        else
        {
            Console.Write(output);
        }
        
        if (exception == null)
        {
            Console.WriteLine();
            return;
        }
        ExceptionOccurred = true;
        ExceptionsList.AddLast(exception);
        Console.WriteLine($"\nException: {exception.Message}\nStack Trace: {exception.StackTrace}");
        
        if (!shouldExit || SuppressProcessExitForTests) return;
        Thread.Sleep(TimeSpan.FromSeconds(5));
        Environment.Exit(1);
    }

    /// <summary>
    /// Writes a line to the console with a pretext based on the output type.
    /// </summary>
    /// <param name="output">Output</param>
    /// <param name="currentStep">Current step, default is none</param>
    /// <param name="outputType">Output type, default is info</param>
    /// <param name="exception">Exception, default is null</param>
    /// <param name="shouldBypassDebug">Should the console Log Bypass Debug Mode</param>
    /// <param name="shouldExit">Should the Program Exit</param>
    /// <typeparam name="T">Any type</typeparam>
    /// <returns>The length of the pretext</returns>
    public static void WriteLineWithStepPretext<T>(T output, CurrentStep currentStep = CurrentStep.None, OutputType outputType = OutputType.Info, Exception? exception = null, bool shouldBypassDebug = false, bool shouldExit = false)
    {
        if (!Program.Config.Debug && !shouldBypassDebug) return;
        
        CurrentTime();
        WriteStep(currentStep);
        WriteOutputType(outputType);
        
        if (output is IEnumerable enumerable && !(output is string))
        {
            Console.Write(string.Join(", ", enumerable.Cast<object>()));
        }
        else
        {
            Console.Write(output);
        }
        
        if (exception == null)
        {
            Console.WriteLine();
            return;
        }
        ExceptionOccurred = true;
        ExceptionsList.AddLast(exception);
        Console.WriteLine($"\nException: {exception.Message}\nStack Trace: {exception.StackTrace}");
                  
        if (!shouldExit || SuppressProcessExitForTests) return;
        Thread.Sleep(TimeSpan.FromSeconds(5));
        Environment.Exit(1);
    }

    /// <summary>
    /// Writes a single line to the console with a pretext based on the output type.
    /// </summary>
    /// <param name="output">Output</param>
    /// <param name="outputType">Output type, default is info</param>
    /// <param name="exception">Exception, default is null</param>
    /// <param name="shouldBypassDebug">Should the console Log Bypass Debug Mode</param>
    /// <param name="shouldExit">Should the Program Exit</param>
    /// <typeparam name="T">Any type</typeparam>
    /// <returns>The length of the pretext</returns>
    public static void WriteWithPretext<T>(T output, OutputType outputType = OutputType.Info, Exception? exception = null, bool shouldBypassDebug = false, bool shouldExit = false)
    {
        if (!Program.Config.Debug && !shouldBypassDebug) return;
        
        CurrentTime();
        WriteOutputType(outputType);
        if (output is IEnumerable enumerable && !(output is string))
        {
            Console.Write(string.Join(", ", enumerable.Cast<object>()));
        }
        else
        {
            Console.Write(output);
        }
        
        if (exception == null)
        {
            Console.WriteLine();
            return;
        }
        ExceptionOccurred = true;
        ExceptionsList.AddLast(exception);
        Console.WriteLine($"\nException: {exception.Message}\nStack Trace: {exception.StackTrace}");
        
        if (!shouldExit || SuppressProcessExitForTests) return;
        Thread.Sleep(TimeSpan.FromSeconds(5));
        Environment.Exit(1);
    }

    /// <summary>
    /// Determines the output type and writes it in the appropriate color.
    /// </summary>
    /// <param name="outputType">Output type</param>
    /// <returns>The length of the pretext</returns>
    private static void WriteOutputType(OutputType outputType)
    {
        var (color, label) = outputType switch
        {
            OutputType.Error => (ConsoleColor.DarkRed, "Error"),
            OutputType.Warning => (ConsoleColor.DarkYellow, "Warning"),
            OutputType.Debug => (ConsoleColor.DarkMagenta, "Debug"),
            _ => (ConsoleColor.Green, "Info")
        };
        
        Console.ForegroundColor = color;
        Console.Write($"[{label}] ");
        Console.ResetColor();
    }

    private static void CurrentTime()
    {
        var dateTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"[{dateTime}] ");
        Console.ResetColor();
    }

    /// <summary>
    /// Determines the current step and returns the length of the pretext.
    /// </summary>
    /// <param name="step">Current step</param>
    /// <returns>The length of the pretext</returns>
    private static void WriteStep(CurrentStep step)
    {
        if (step == CurrentStep.None) return;

        var label = step switch
        {
            CurrentStep.FileChecks => "FileChecks",
            CurrentStep.FileReading => "File Reading",
            CurrentStep.MessageHistory => "Message History",
            CurrentStep.PelicanApi => "Pelican API",
            CurrentStep.A2SQuery => "A2S Query",
            CurrentStep.RconQuery => "RCON Query",
            CurrentStep.MinecraftJavaQuery => "MC Java Query",
            CurrentStep.MinecraftBedrockQuery => "MC Bedrock Query",
            CurrentStep.DiscordMessage => "Discord Message",
            CurrentStep.DiscordInteraction => "Discord Interaction",
            CurrentStep.Updater => "Updater",
            CurrentStep.Markdown => "Markdown",
            CurrentStep.Helper => "Helper",
            CurrentStep.GameMonitoring => "Game Monitoring",
            CurrentStep.EmbedBuilding => "Embed Building",
            CurrentStep.Initialization => "Initialization",
            _ => step.ToString()
        };

        Console.Write($"[{label}] ");
    }
}