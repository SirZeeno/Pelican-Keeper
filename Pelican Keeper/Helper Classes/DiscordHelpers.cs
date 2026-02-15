using DSharpPlus.Entities;

namespace Pelican_Keeper.Helper_Classes;

public static class DiscordHelpers
{
    /// <summary>
    /// Checks the size of each individual component of the embed and compares it to its max size for each component.
    /// </summary>
    /// <param name="embed">Discord Embed</param>
    /// <returns>True if Size Passes and false if it doesn't</returns>
    public static bool EmbedLengthCheck(DiscordEmbed embed)
    {
        // Per-embed limits in characters
        // Title: 256
        // Description: 4096
        // Field name: 256
        // Field value: 1024
        // Footer text: 2048
        // Author name: 256
        // Total embed characters: 6000
        
        bool sizePasses = true;
        int fullSize = 0;

        if (embed.Title is { Length: > 256 })
        {
            sizePasses = false;
            fullSize += embed.Title.Length;
            ConsoleExt.WriteLine($"Title length: {embed.Title.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        }
        
        if (embed.Description is { Length: > 4096 })
        {
            sizePasses = false;
            fullSize += embed.Description.Length;
            ConsoleExt.WriteLine($"Description length: {embed.Description.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        }
        
        foreach (var field in embed.Fields)
        {
            if (field.Name is { Length: > 256 })
            {
                sizePasses = false;
                fullSize += field.Name.Length;
                ConsoleExt.WriteLine($"Field's {field.Name} Name length: {field.Name.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
            }
            if  (field.Value is { Length: > 1024 })
            {
                sizePasses = false;
                fullSize += field.Value.Length;
                ConsoleExt.WriteLine($"Field's {field.Name} Value length: {field.Value.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
            }
        }

        if (embed.Footer?.Text is { Length: > 2048 })
        {
            sizePasses = false;
            fullSize += embed.Footer.Text.Length;
            ConsoleExt.WriteLine($"Footer Text length: {embed.Footer.Text.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        }

        if (embed.Author?.Name is { Length: > 256 })
        {
            sizePasses = false;
            fullSize += embed.Author.Name.Length;
            ConsoleExt.WriteLine($"Author Name length: {embed.Author.Name.Length}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        }

        if (fullSize > 6000)
        {
            sizePasses = false;
            ConsoleExt.WriteLine($"Full Size Embed length: {fullSize}", ConsoleExt.CurrentStep.EmbedBuilding, ConsoleExt.OutputType.Debug);
        }
        
        return sizePasses;
    }
    
    /// <summary>
    /// Puts a list of DiscordComponent's each into one line
    /// </summary>
    /// <param name="mb">DiscordMessageBuilder</param>
    /// <param name="components">List of DiscordComponent's</param>
    /// <param name="maxRows">Maximum number of rows you allow, Default 5</param>
    public static void AddRows(this DiscordMessageBuilder mb, IEnumerable<DiscordComponent> components, int maxRows = 5)
    {
        var rowsUsed = 0;
        var buttonBuffer = new List<DiscordComponent>(capacity: 5);

        void FlushButtons()
        {
            if (buttonBuffer.Count == 0) return;
            // pack buttons in rows of up to 5
            foreach (var chunk in buttonBuffer.Chunk(5))
            {
                if (rowsUsed >= maxRows) return;
                mb.AddComponents(chunk);
                rowsUsed++;
            }
            buttonBuffer.Clear();
        }

        foreach (var comp in components)
        {
            switch (comp)
            {
                case DiscordSelectComponent select:
                    FlushButtons();
                    if (rowsUsed >= maxRows) return;
                    // a select must be the only item in its row otherwise discord will freak out for some weird reason
                    mb.AddComponents(select);
                    rowsUsed++;
                    break;

                default:
                    // everything else gets treated as a button-like component
                    buttonBuffer.Add(comp);
                    // if it accumulated 5, it will flush a row
                    if (buttonBuffer.Count == 5)
                        FlushButtons();
                    break;
            }

            if (rowsUsed >= maxRows) break;
        }

        // flush any remaining buttons at the end
        if (rowsUsed < maxRows)
            FlushButtons();
    }
}