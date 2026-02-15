using DSharpPlus.Entities;

namespace Pelican_Keeper.Helper_Classes;

public static class DebugHelpers
{
    /// <summary>
    /// A Console Dump for a list of DiscordComponent's and their contents
    /// </summary>
    /// <param name="comps">List of DiscordComponent's</param>
    public static void DebugDumpComponents(IEnumerable<DiscordComponent> comps)
    {
        int rows = 0, total = 0;
        var buffer = new List<DiscordComponent>(5);

        void FlushButtons()
        {
            if (buffer.Count == 0) return;
            rows++;
            ConsoleExt.WriteLine($"[ROW {rows}] {buffer.Count} button(s)", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
            foreach (var b in buffer)
            {
                if (b is DiscordButtonComponent btn)
                {
                    ConsoleExt.WriteLine($"  • Button: style={btn.Style}, label='{btn.Label}' len={btn.Label?.Length ?? 0}, custom_id='{btn.CustomId}' len={btn.CustomId?.Length ?? 0}, type='{btn.Type}'", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
                    total++;
                }
            }
            buffer.Clear();
        }

        foreach (var c in comps)
        {
            switch (c)
            {
                case DiscordSelectComponent s:
                    // Buttons row before a select
                    FlushButtons();
                    rows++;
                    ConsoleExt.WriteLine($"[ROW {rows}] 1 select: custom_id='{s.CustomId}', placeholder='{s.Placeholder}' len={s.Placeholder?.Length ?? 0}, options={s.Options?.Count}", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
                    if (s.Options != null)
                    {
                        for (int i = 0; i < s.Options.Count; i++)
                        {
                            var o = s.Options[i];
                            ConsoleExt.WriteLine($"    - opt[{i}]: label='{o.Label}' len={o.Label.Length}, value='{o.Value}' len={o.Value.Length}, desc len={o.Description?.Length ?? 0}", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
                            total++;
                        }
                    }
                    break;

                case DiscordButtonComponent b:
                    buffer.Add(b);
                    if (buffer.Count == 5) FlushButtons();
                    break;

                default:
                    ConsoleExt.WriteLine($"[WARN] Unknown component type: {c.GetType().Name}", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
                    break;
            }
        }
        FlushButtons();
        ConsoleExt.WriteLine($"[SUMMARY] rows={rows}, total components={total}", ConsoleExt.CurrentStep.DiscordInteraction, ConsoleExt.OutputType.Debug);
    }
}