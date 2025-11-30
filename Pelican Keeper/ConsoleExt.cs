using System.Collections;

namespace Pelican_Keeper;

//TODO: be more selective with the output to only show what is relevant and reduce the output unless debug and verbose is enabled
public static class ConsoleExt
{
    public static bool ExceptionOccurred;
    public static IReadOnlyCollection<Exception> Exceptions => _exceptions;
    // ReSharper disable once InconsistentNaming
    private static readonly LinkedList<Exception> _exceptions = new();
    
    public enum OutputType
    {
        Error,
        Info,
        Warning,
        Question
    }

    //TODO: Update all the current console outputs to use these enums for better logging and tracking of issues and steps
    // This is changeable to be whatever you want
    public enum CurrentStep
    {
        FileChecks,
        MessageHistory,
        PelicanApiRequest,
        A2SRequest,
        RconRequest,
        MinecraftJavaRequest,
        MinecraftBedrockRequest,
        DiscordMessage,
        DiscordReaction,
        Updater,
        Markdown,
        Helper,
        GameMonitoring,
        EmbedBuilding,
        FileReading,
        None
    }

    /// <summary>
    /// Writes a line to the console with a pretext based on the output type.
    /// </summary>
    /// <param name="output">Output</param>
    /// <param name="outputType">Output type, default is info</param>
    /// <param name="exception">Exception, default is null</param>
    /// <param name="exit">Should the Program Exit</param>
    /// <typeparam name="T">Any type</typeparam>
    /// <returns>The length of the pretext</returns>
    public static void WriteLineWithPretext<T>(T output, OutputType outputType = OutputType.Info, Exception? exception = null, bool exit = false)
    {
        var length1 = CurrentTime();
        var length2 = DetermineOutputType(outputType);
        if (output is IEnumerable enumerable && !(output is string))
        {
            Console.WriteLine(string.Join(", ", enumerable.Cast<object>()));
        }
        else
        {
            Console.WriteLine(output);
        }
        var constructedReturn = (length1.length + length2.length, length1.pretext + length2.pretext + output);
        
        if (exception == null) return;
        ExceptionOccurred = true;
        _exceptions.AddLast(exception);
        constructedReturn.Item2 += $"\nException: {exception.Message}\nStack Trace: {exception.StackTrace}";
        if (!exit) return;
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
    /// <param name="exit">Should the Program Exit</param>
    /// <typeparam name="T">Any type</typeparam>
    /// <returns>The length of the pretext</returns>
    public static void WriteLineWithStepPretext<T>(T output, CurrentStep currentStep = CurrentStep.None, OutputType outputType = OutputType.Info, Exception? exception = null, bool exit = false)
    {
        var length1 = CurrentTime();
        var length2 = DetermineCurrentStep(currentStep);
        var length3 = DetermineOutputType(outputType);
        if (output is IEnumerable enumerable && !(output is string))
        {
            Console.WriteLine(string.Join(", ", enumerable.Cast<object>()));
        }
        else
        {
            Console.WriteLine(output);
        }
        var constructedReturn = (length1.length + length2.length + length3.length, length1.pretext + length2.pretext + length3.pretext + output);
        
        if (exception == null) return;
        ExceptionOccurred = true;
        _exceptions.AddLast(exception);
        constructedReturn.Item2 += $"\nException: {exception.Message}\nStack Trace: {exception.StackTrace}";
        if (!exit) return;
        Thread.Sleep(TimeSpan.FromSeconds(5));
        Environment.Exit(1);
    }

    /// <summary>
    /// Writes a single line to the console with a pretext based on the output type.
    /// </summary>
    /// <param name="output">Output</param>
    /// <param name="outputType">Output type, default is info</param>
    /// <param name="exception">Exception, default is null</param>
    /// <typeparam name="T">Any type</typeparam>
    /// <returns>The length of the pretext</returns>
    public static void WriteWithPretext<T>(T output, OutputType outputType = OutputType.Info, Exception? exception = null)
    {
        var length1 = CurrentTime();
        var length2 = DetermineOutputType(outputType);
        if (output is IEnumerable enumerable && !(output is string))
        {
            Console.WriteLine(string.Join(", ", enumerable.Cast<object>()));
        }
        else
        {
            Console.Write(output);
        }
        var constructedReturn = (length1.length + length2.length, length1.pretext + length2.pretext + output);
        
        if (exception == null) return;
        ExceptionOccurred = true;
        _exceptions.AddLast(exception);
        constructedReturn.Item2 += $"\nException: {exception.Message}\nStack Trace: {exception.StackTrace}";
    }

    /// <summary>
    /// Determines the output type and returns the length of the pretext.
    /// </summary>
    /// <param name="outputType">Output type</param>
    /// <returns>The length of the pretext</returns>
    private static (int length, string pretext) DetermineOutputType(OutputType outputType)
    {
        return outputType switch
        {
            OutputType.Error => CreateOutputType(nameof(OutputType.Error), ConsoleColor.DarkRed),
            OutputType.Info => CreateOutputType(nameof(OutputType.Info), ConsoleColor.Green),
            OutputType.Warning => CreateOutputType(nameof(OutputType.Info), ConsoleColor.DarkYellow),
            OutputType.Question => CreateOutputType(nameof(OutputType.Info), ConsoleColor.DarkGreen),
            _ => (0, String.Empty)
        };
    }

    /// <summary>
    /// Creates the pretext for the output type.
    /// </summary>
    /// <param name="outputType">Output type</param>
    /// <param name="consoleColor">Console color to use for the pretext</param>
    /// <returns>The length of the pretext</returns>
    private static (int length, string pretext) CreateOutputType(string outputType, ConsoleColor consoleColor)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = consoleColor;
        Console.Write($"[{outputType}] ");
        Console.ForegroundColor = oldColor;
        return (outputType.Length, $"[{outputType}] ");
    }

    private static (int length, string pretext) CurrentTime()
    {
        var dateTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"[{dateTime}] ");
        Console.ForegroundColor = oldColor;
        return (dateTime.Length + 3, $"[{dateTime}] ");
    }

    /// <summary>
    /// Determines the current step and returns the length of the pretext.
    /// </summary>
    /// <param name="currentStep">Current step</param>
    /// <returns>The length of the pretext</returns>
    private static (int length, string pretext) DetermineCurrentStep(CurrentStep currentStep)
    {
        return currentStep switch
        {
            CurrentStep.FileChecks => CreateCurrentStep("Files Integrity Check"),
            CurrentStep.MessageHistory => CreateCurrentStep("Message History"),
            CurrentStep.PelicanApiRequest => CreateCurrentStep("Pelican API Request"),
            CurrentStep.A2SRequest => CreateCurrentStep("A2S Query"),
            CurrentStep.RconRequest => CreateCurrentStep("RCON Request"),
            CurrentStep.MinecraftJavaRequest => CreateCurrentStep("Minecraft Java Request"),
            CurrentStep.MinecraftBedrockRequest => CreateCurrentStep("Minecraft Bedrock Request"),
            CurrentStep.DiscordMessage => CreateCurrentStep("Discord Message"),
            CurrentStep.DiscordReaction=> CreateCurrentStep("Discord Reaction"),
            CurrentStep.Updater => CreateCurrentStep("Updater"),
            CurrentStep.Markdown => CreateCurrentStep("Markdown"),
            CurrentStep.Helper => CreateCurrentStep("Helper"),
            CurrentStep.GameMonitoring => CreateCurrentStep("Game Monitoring"),
            CurrentStep.EmbedBuilding => CreateCurrentStep("Embed Building"),
            CurrentStep.FileReading => CreateCurrentStep("File Reading"),
            _ => (0, String.Empty)
        };
    }

    /// <summary>
    /// Creates the pretext for the current step.
    /// </summary>
    /// <param name="currentStep">Current step</param>
    /// <returns>The length of the pretext</returns>
    private static (int length, string pretext) CreateCurrentStep(string currentStep)
    {
        Console.Write($"[{currentStep}] ");
        return (currentStep.Length, $"[{currentStep}] ");
    }
}