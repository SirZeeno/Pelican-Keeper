using System.Text.Json.Serialization;

namespace Pelican_Keeper;

public abstract class TemplateClasses
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageFormat
    {
        PerServer,
        Consolidated,
        Paginated,
        None
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageSorting
    {
        Name,
        Status,
        Uptime,
        None
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageSortingDirection
    {
        Ascending,
        Descending,
        None
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CommandExecutionMethod
    {
        MinecraftJava,
        MinecraftBedrock,
        Rcon,
        A2S,
        Terraria
    }

    public enum ServerStatus
    {
        Online,
        Offline,
        Paused,
        Starting,
        Stopping
    }

    public enum ReleasePlatform
    {
        WinX86,
        WinX64,
        LinuxX64,
        LinuxArm,
        LinuxArm64,
        OsxX64,
        OsxArm64,
        None
    }

    public record Secrets
    (
        string? ClientToken,
        string? ServerToken,
        string? ServerUrl,
        string? BotToken,
        ulong[]? ChannelIds,
        string? ExternalServerIp
    );

    public class Config
    {
        public string? InternalIpStructure { get; set; }
        public MessageFormat MessageFormat { get; set; } = MessageFormat.None;
        public MessageSorting MessageSorting { get; set; } = MessageSorting.None;
        public MessageSortingDirection MessageSortingDirection { get; set; } = MessageSortingDirection.None;
        public bool IgnoreOfflineServers { get; set; }
        public bool IgnoreInternalServers { get; set; }
        public string[]? ServersToIgnore { get; set; }

        public bool JoinableIpDisplay { get; set; }
        public bool PlayerCountDisplay { get; set; }
        public string[]? ServersToMonitor { get; set; }

        public bool AutomaticShutdown { get; set; }
        public string[]? ServersToAutoShutdown { get; set; }
        public string? EmptyServerTimeout { get; set; }
        public bool AllowUserServerStartup { get; set; }
        public string[]? AllowServerStartup { get; set; }
        public string[]? UsersAllowedToStartServers { get; set; }
        public bool AllowUserServerStopping { get; set; }
        public string[]? AllowServerStopping { get; set; }
        public string[]? UsersAllowedToStopServers { get; set; }

        public bool ContinuesMarkdownRead { get; set; }
        public bool ContinuesGamesToMonitorRead { get; set; }
        public int MarkdownUpdateInterval { get; set; }

        private int _serverUpdateInterval;
        public int ServerUpdateInterval
        {
            get => _serverUpdateInterval;
            set => _serverUpdateInterval = Math.Max(value, 10);
        }

        public bool LimitServerCount { get; set; }
        public int MaxServerCount { get; set; }
        public string[]? ServersToDisplay { get; set; }

        public bool Debug { get; set; }
        public bool DryRun { get; set; }
        public bool AutoUpdate { get; set; }
    }

    public class ServerInfo
    {
        public int Id { get; init; }
        public string Uuid { get; init; } = null!;
        public string Name { get; init; } = null!;
        public EggInfo Egg { get; init; } = null!;
        public long MaxMemoryBytes { get; set; }
        public ServerResources? Resources { get; set; }
        public List<ServerAllocation>? Allocations { get; set; }
        public string? PlayerCountText { get; set; }
    }

    public class ServerResources
    {
        public string CurrentState { get; init; } = null!;
        public long MemoryBytes { get; init; }
        public double CpuAbsolute { get; init; }
        public long DiskBytes { get; init; }
        public long NetworkRxBytes { get; init; }
        public long NetworkTxBytes { get; init; }
        public long Uptime { get; init; }
    }

    public class ServerAllocation
    {
        public string Uuid { get; init; } = null!;
        public string Ip { get; init; } = null!;
        public int Port { get; init; }
        public bool IsDefault { get; init; }
    }

    public class LiveMessageJsonStorage
    {
        public HashSet<ulong>? LiveStore { get; set; } = new();
        public Dictionary<ulong, int>? PaginatedLiveStore { get; set; } = new();
    }

    public class ServerViewModel
    {
        public string PlayerCount { get; set; } = null!;
        public string IpAndPort { get; set; } = null!;
        public string? Uuid { get; set; }
        public string ServerName { get; init; } = null!;
        public string Status { get; set; } = null!;
        public string StatusIcon { get; set; } = null!;
        public string Cpu { get; set; } = null!;
        public string Memory { get; set; } = null!;
        public string Disk { get; set; } = null!;
        public string NetworkRx { get; set; } = null!;
        public string NetworkTx { get; set; } = null!;
        public string Uptime { get; set; } = null!;
    }

    public class GamesToMonitor
    {
        public string Game { get; init; } = null!;
        public CommandExecutionMethod Protocol { get; init; }
        public string? RconPortVariable { get; set; }
        public string? RconPasswordVariable { get; set; }
        public string? RconPassword { get; set; }
        public string? Command { get; set; }
        public string? QueryPortVariable { get; set; }
        public string? MaxPlayerVariable { get; set; }
        public string? MaxPlayer { get; set; }
        public string? PlayerCountExtractRegex { get; set; }
    }

    public class EggInfo
    {
        public int Id { get; init; }
        public string Name { get; set; } = null!;
    }
}