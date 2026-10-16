using System.Text.RegularExpressions;
using static Pelican_Keeper.TemplateClasses;

namespace Pelican_Keeper.Helper_Classes;

public static class ServerListHelpers
{
    /// <summary>
    ///     Filters the Server list with the settings defined in the config
    /// </summary>
    /// <param name="servers">Server Info list</param>
    public static List<ServerInfo> FilterServerList(List<ServerInfo> servers)
    {
        var serversToIgnore = Program.Config.ServersToIgnore;
        if (serversToIgnore is { Length: > 0 } && serversToIgnore[0] != "UUIDS HERE")
            servers = servers.Where(s => !serversToIgnore.Contains(s.Uuid)).ToList();

        servers = HelperClass.SortServers(servers, Program.Config.MessageSorting, Program.Config.MessageSortingDirection);
        
        if (Program.Config.IgnoreInternalServers && Program.Config.InternalIpStructure != null)
        {
            var internalIpPattern = "^" + Regex.Escape(Program.Config.InternalIpStructure).Replace("\\*", "\\d+") + "$";
            servers = servers.Where(s => !(s.Allocations?.All(a => Regex.IsMatch(a.Ip, internalIpPattern)) ?? false))
                .ToList();
        }

        return servers;
    }

    public static List<ServerInfo> LimitServerList(List<ServerInfo> servers)
    {
        if (!Program.Config.LimitServerCount || Program.Config.MaxServerCount <= 0 ||
            Program.Config.MessageFormat == MessageFormat.Paginated) return servers;
        
        if (Program.Config.ServersToDisplay != null && Program.Config.ServersToDisplay.Length > 0 &&
            Program.Config.ServersToDisplay[0] != "UUIDS HERE")
            servers = servers.Where(s => Program.Config.ServersToDisplay.Contains(s.Uuid)).ToList();
        else
            servers = servers.Take(Program.Config.MaxServerCount).ToList();
        return servers;
    }
}