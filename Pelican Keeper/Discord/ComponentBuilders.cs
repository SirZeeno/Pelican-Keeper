using DSharpPlus;
using DSharpPlus.Entities;
using Pelican_Keeper.Models;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Discord;

/// <summary>
/// Builds Discord UI components (buttons, dropdowns) for server control.
/// </summary>
public static class ComponentBuilders
{
    /// <summary>
    /// Builds start/stop dropdown menus for Consolidated mode.
    /// </summary>
    public static List<DiscordComponent> BuildServerDropdowns(List<ServerInfo> servers, Config config)
    {
        var components = new List<DiscordComponent>();
        var showStart = config is { AllowUserServerStartup: true, IgnoreOfflineServers: false };
        var showStop = config.AllowUserServerStopping;
        var uuids = servers.Select(s => s.Uuid).ToList();

        if (showStart)
        {
            var options = servers.Select((s, i) => new DiscordSelectComponentOption(s.Name, uuids[i]));
            foreach (var group in CollectionHelper.Chunk(options, 25))
                components.Add(new DiscordSelectComponent("start_menu", "Start a server…", group));
        }

        if (showStop)
        {
            var options = servers.Select((s, i) => new DiscordSelectComponentOption(s.Name, uuids[i]));
            foreach (var group in CollectionHelper.Chunk(options, 25))
                components.Add(new DiscordSelectComponent("stop_menu", "Stop a server…", group));
        }

        return components;
    }

    /// <summary>
    /// Builds start/stop buttons for PerServer mode.
    /// </summary>
    public static List<DiscordButtonComponent> BuildServerButtons(string uuid, Config config)
    {
        var components = new List<DiscordButtonComponent>();
        var showStart = config is { AllowUserServerStartup: true, IgnoreOfflineServers: false };
        var showStop = config.AllowUserServerStopping;

        if (showStart)
            components.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"start:{uuid}", "Start"));

        if (showStop)
            components.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"stop:{uuid}", "Stop"));

        return components;
    }

    /// <summary>
    /// Builds pagination buttons with optional start/stop for Paginated mode.
    /// </summary>
    public static List<DiscordButtonComponent> BuildPaginationButtons(string? serverUuid, Config config)
    {
        var showStart = config is { AllowUserServerStartup: true, IgnoreOfflineServers: false };
        var showStop = config.AllowUserServerStopping;

        var components = new List<DiscordButtonComponent>
        {
            new(ButtonStyle.Primary, "prev_page", "◀️ Previous")
        };

        if (showStart && serverUuid != null)
            components.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"start:{serverUuid}", "Start"));

        if (showStop && serverUuid != null)
            components.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"stop:{serverUuid}", "Stop"));

        components.Add(new DiscordButtonComponent(ButtonStyle.Primary, "next_page", "Next ▶️"));

        return components;
    }

    /// <summary>
    /// Adds components to a message builder, respecting Discord's row limits.
    /// </summary>
    public static DiscordMessageBuilder AddComponentRows(this DiscordMessageBuilder builder, IEnumerable<DiscordComponent> components, int maxRows = 5)
    {
        var rowsUsed = 0;
        var buttonBuffer = new List<DiscordComponent>(5);

        void FlushButtons()
        {
            if (buttonBuffer.Count == 0) return;
            foreach (var chunk in buttonBuffer.Chunk(5))
            {
                if (rowsUsed >= maxRows) return;
                builder.AddComponents(chunk);
                rowsUsed++;
            }
            buttonBuffer.Clear();
        }

        foreach (var comp in components)
        {
            if (comp is DiscordSelectComponent select)
            {
                FlushButtons();
                if (rowsUsed >= maxRows) return builder;
                builder.AddComponents(select);
                rowsUsed++;
            }
            else
            {
                buttonBuffer.Add(comp);
                if (buttonBuffer.Count == 5)
                    FlushButtons();
            }

            if (rowsUsed >= maxRows) break;
        }

        if (rowsUsed < maxRows)
            FlushButtons();

        return builder;
    }
}
