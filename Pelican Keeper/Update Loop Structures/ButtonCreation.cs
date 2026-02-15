using DSharpPlus;
using DSharpPlus.Entities;

namespace Pelican_Keeper.Update_Loop_Structures;

using static TemplateClasses;
using static ConsoleExt;
using static HelperClass;

public static class ButtonCreation
{
    static readonly Config Config = Program.Config;
    
    public static List<DiscordComponent> ConsolidatedButtonCreation(List<string?> uuids)
    {
        List<string?> selectedServerUuids = uuids;
                                
        if (Config.AllowServerStartup is { Length: > 0 } && !string.Equals(Config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal))
        {
            selectedServerUuids = selectedServerUuids.Where(uuid => Config.AllowServerStartup.Contains(uuid)).ToList();
            WriteLine($"Selected Servers: {selectedServerUuids.Count}", CurrentStep.None, OutputType.Warning);
        }
        
        if (Config.AllowServerStopping is { Length: > 0 } && !string.Equals(Config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal))
        {
            selectedServerUuids = selectedServerUuids.Where(uuid => Config.AllowServerStopping.Contains(uuid)).ToList();
        }
        
        List<DiscordComponent> buttons = [];
                                
        // Build START menus
        if (Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false })
        {
            var startOptions = selectedServerUuids.Select((uuid, i) =>
                new DiscordSelectComponentOption(
                    label: Program.GlobalServerInfo[i].Name,     // shown to user
                    value: uuid                     // data you read on interaction
                )
            );

            foreach (var group in Chunk(startOptions, 25))
            {
                var startMenu = new DiscordSelectComponent(
                    customId: "start_menu",
                    placeholder: "Start a server…",
                    options: group,
                    minOptions: 1,
                    maxOptions: 1,
                    disabled: false
                );
                buttons.Add(startMenu);
            }
        }
                                
        // Build STOP menus
        if (Config.AllowUserServerStopping)
        {
            var stopOptions = selectedServerUuids.Select((uuid, i) =>
                new DiscordSelectComponentOption(
                    label: Program.GlobalServerInfo[i].Name,
                    value: uuid
                )
            );

            foreach (var group in Chunk(stopOptions, 25))
            {
                var stopMenu = new DiscordSelectComponent(
                    customId: "stop_menu",
                    placeholder: "Stop a server…",
                    options: group,
                    minOptions: 1,
                    maxOptions: 1,
                    disabled: false
                );
                buttons.Add(stopMenu);
            }
        }

        WriteLine("Buttons created: " + buttons.Count, CurrentStep.DiscordMessage, OutputType.Debug);
        DebugDumpComponents(buttons);
        return buttons;
    }
    
    public static List<DiscordComponent> PaginatedButtonCreation(List<string?> uuids, int index)
    {
        if (index < 0 || index >= uuids.Count)
        {
            WriteLine("Page Index out of range: " + index, CurrentStep.DiscordInteraction, OutputType.Error, new ArgumentOutOfRangeException(nameof(index)), true, true);
        }
        
        // treat "UUIDS HERE" placeholder or empty/null list as "allow all"
        bool allowAllStart = Config.AllowServerStartup == null || Config.AllowServerStartup.Length == 0 || string.Equals(Config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal);
        WriteLine("show all Start: " + allowAllStart, CurrentStep.DiscordInteraction, OutputType.Debug);

        // allow only if user-startup enabled, not ignoring offline, and either allow-all or in allow-list
        bool showStart = Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false, AllowServerStartup: not null } && (allowAllStart || Config.AllowServerStartup.Contains(uuids[index], StringComparer.OrdinalIgnoreCase));
        WriteLine("show Start: " + showStart, CurrentStep.DiscordInteraction, OutputType.Debug);
            
        // treat "UUIDS HERE" placeholder or empty/null list as "allow all"
        bool allowAllStop = Config.AllowServerStopping == null || Config.AllowServerStopping.Length == 0 || string.Equals(Config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal);
        WriteLine("show all Stop: " + allowAllStop, CurrentStep.DiscordInteraction, OutputType.Debug);

        // allow only if user-startup enabled, not ignoring offline, and either allow-all or in stop-list
        bool showStop = Config is { AllowUserServerStopping: true, IgnoreOfflineServers: false, AllowServerStopping: not null } && (allowAllStop || Config.AllowServerStopping.Contains(uuids[index], StringComparer.OrdinalIgnoreCase));
        WriteLine("show Stop: " + showStop, CurrentStep.DiscordInteraction, OutputType.Debug);
        
        List<DiscordComponent> buttons =
        [
            new DiscordButtonComponent(ButtonStyle.Primary, "prev_page", "◀️ Previous")
        ];
        if (showStart)
            buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary,$"Start: {uuids[index]}", "Start"));
        
        if (showStop)
            buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"Stop: {uuids[index]}", "Stop")); // The CustomId can never be the same.
        buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, "next_page", "Next ▶️"));

        return buttons;
    }

    public static List<DiscordComponent> PerServerButtonCreation(string? uuid)
    {
        // treat "UUIDS HERE" placeholder or empty/null list as "allow all"
        bool allowAllStart = Config.AllowServerStartup == null || Config.AllowServerStartup.Length == 0 || string.Equals(Config.AllowServerStartup[0], "UUIDS HERE", StringComparison.Ordinal);
        WriteLine("show all Start: " + allowAllStart, CurrentStep.DiscordInteraction, OutputType.Debug);

        // allow only if user-startup enabled, not ignoring offline, and either allow-all or in allow-list
        bool showStart = Config is { AllowUserServerStartup: true, IgnoreOfflineServers: false, AllowServerStartup: not null } && (allowAllStart || Config.AllowServerStartup.Contains(uuid, StringComparer.OrdinalIgnoreCase));
        WriteLine("show Start: " + showStart, CurrentStep.DiscordInteraction, OutputType.Debug);
            
        // treat "UUIDS HERE" placeholder or empty/null list as "allow all"
        bool allowAllStop = Config.AllowServerStopping == null || Config.AllowServerStopping.Length == 0 || string.Equals(Config.AllowServerStopping[0], "UUIDS HERE", StringComparison.Ordinal);
        WriteLine("show all Stop: " + allowAllStop, CurrentStep.DiscordInteraction, OutputType.Debug);

        // allow only if user-startup enabled, not ignoring offline, and either allow-all or in stop-list
        bool showStop = Config is { AllowUserServerStopping: true, IgnoreOfflineServers: false, AllowServerStopping: not null } && (allowAllStop || Config.AllowServerStopping.Contains(uuid, StringComparer.OrdinalIgnoreCase));
        WriteLine("show Stop: " + showStop, CurrentStep.DiscordInteraction, OutputType.Debug);

        List<DiscordComponent> buttons = [];
        if (uuid == null) return buttons;
        if (showStart)
            buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"Start: {uuid}", "Start"));
        if (showStop)
            buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"Stop: {uuid}", "Stop"));

        return buttons;
    }
}