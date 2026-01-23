using Newtonsoft.Json;
using Pelican_Keeper;

namespace Pelican_Keeper_Unit_Testing;

public class LiveMessageTesting
{
    private string? _messageHistoryFilePath;
    private readonly ulong _messageId = 0393093003;
    
    [SetUp]
    public void Setup()
    {
        ConsoleExt.SuppressProcessExitForTests = true;
        
        DirectoryInfo? directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
        if (directoryInfo.Parent?.Parent?.Parent?.Parent?.Parent?.Exists != false)
            directoryInfo = directoryInfo.Parent?.Parent?.Parent?.Parent?.Parent;

        bool pelicanKeeperExists = false;
        if (directoryInfo == null) return;
        
        DirectoryInfo[] childDirectories = directoryInfo.GetDirectories();
        foreach (var childDirectory in childDirectories) if (childDirectory.Name == "Pelican Keeper") pelicanKeeperExists = true;

        if (pelicanKeeperExists) directoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "Pelican Keeper"));
        
        FileInfo[] fileInfos = directoryInfo.GetFiles();
        foreach (var fileInfo in fileInfos) if (fileInfo.Name == "MessageHistory.json") _messageHistoryFilePath =  fileInfo.FullName;

        LiveMessageStorage.LoadAll(_messageHistoryFilePath);
    }
    
    [Test, Order(1)]
    public void WritingLiveHistory()
    {
        try
        {
            LiveMessageStorage.Save(_messageId);
        }
        catch (Exception e)
        {
            ConsoleExt.WriteLineWithStepPretext($"Message: {e.Message} StackTrace: {e.StackTrace}", ConsoleExt.CurrentStep.FileReading, ConsoleExt.OutputType.Error, e, true, true);
            Assert.Fail("Failed to write message ID!\n");
            return;
        }
        Assert.Pass("Message ID saved successfully!\n");
    }
    
    [Test, Order(2)]
    public void ReadingLiveHistory()
    {
        ulong? messageId = null;
        try
        {
            messageId = LiveMessageStorage.Get(_messageId);
            if (messageId == null) Assert.Fail("Failed to retrieve message ID from message history!\n");
        }
        catch (Exception e)
        {
            ConsoleExt.WriteLineWithStepPretext($"Message: {e.Message} StackTrace: {e.StackTrace}", ConsoleExt.CurrentStep.FileReading, ConsoleExt.OutputType.Error, e, true, true);
            Assert.Fail("Failed to retrieve message ID!\n");
        }
        
        Assert.Pass($"Message ID {messageId} retrieved successfully!\n");
    }
    
    [Test, Order(3)]
    public void WritingPaginatedLiveHistory()
    {
        try
        {
            LiveMessageStorage.Save(_messageId, 0);
        }
        catch (Exception e)
        {
            ConsoleExt.WriteLineWithStepPretext(e.Message, ConsoleExt.CurrentStep.FileReading, ConsoleExt.OutputType.Error, e, true, true);
            Assert.Fail("Failed to write message ID and Page Index!\n");
            return;
        }
        
        Assert.Pass("Message ID and Page Index saved successfully!\n");
    }
    
    [Test, Order(4)]
    public void ReadingPaginatedLiveHistory()
    {
        int? pageIndex = null;
        try
        {
            pageIndex = LiveMessageStorage.GetPaginated(_messageId);
            if (pageIndex == null) Assert.Fail("Failed to retrieve Page Index from message history!\n");
        }
        catch (Exception e)
        {
            ConsoleExt.WriteLineWithStepPretext($"Message: {e.Message} StackTrace: {e.StackTrace}", ConsoleExt.CurrentStep.FileReading, ConsoleExt.OutputType.Error, e, true, true);
            Assert.Fail("Failed to retrieve Page Index!\n");
        }
        
        Assert.Pass($"Page Index {pageIndex} retrieved successfully!\n");
    }
    
    [Test, Order(5)]
    public void RemoveMessageHistory()
    {
        try
        {
            LiveMessageStorage.Remove(_messageId);
        }
        catch (Exception e)
        {
            ConsoleExt.WriteLineWithStepPretext($"Message: {e.Message} StackTrace: {e.StackTrace}", ConsoleExt.CurrentStep.FileReading, ConsoleExt.OutputType.Error, e, true, true);
            Assert.Fail("Failed to remove message ID from message history!\n");
            return;
        }
        Assert.Pass("Message ID successfully removed from message history!\n");
    }
}