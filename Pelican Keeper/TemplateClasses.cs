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
        Terraria //TODO: Still need to implement this
    }

    //TODO: for Server status in the template class, so i dont have to compare the literal string but instead use a enum which is more predicatable
    public enum ServerStatus
    {
        Online,
        Offline,
        Paused,
        Starting,
        Stopping
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
        public string? InternalIpStructure { get; init; }
        public MessageFormat MessageFormat { get; set; } = MessageFormat.None;
        public MessageSorting MessageSorting { get; init; } = MessageSorting.None;
        public MessageSortingDirection MessageSortingDirection { get; init; } = MessageSortingDirection.None;
        public bool IgnoreOfflineServers { get; init; }
        public bool IgnoreInternalServers { get; set; }
        public bool IgnoreServersWithoutAllocations { get; init; }
        public string[]? ServersToIgnore { get; set; }
        
        public bool JoinableIpDisplay { get; init; }
        public bool PlayerCountDisplay { get; init; }
        public string[]? ServersToMonitor { get; init; }
        
        public bool AutomaticShutdown { get; init; }
        public string[]? ServersToAutoShutdown { get; init; }
        public string? EmptyServerTimeout { get; init; }
        public bool AllowUserServerStartup { get; init; }
        public string[]? AllowServerStartup { get; init; }
        public string[]? UsersAllowedToStartServers { get; init; }
        public bool AllowUserServerStopping { get; init; }
        public string[]? AllowServerStopping { get; init; }
        public string[]? UsersAllowedToStopServers { get; init; }

        public bool ContinuesMarkdownRead { get; init; }
        public bool ContinuesGamesToMonitorRead { get; init; }
        private readonly int _markdownUpdateInterval;
        public int MarkdownUpdateInterval 
        {
            get => _markdownUpdateInterval;
            init => _markdownUpdateInterval = Math.Max(value, 10);
        }
        private readonly int _serverUpdateInterval;
        public int ServerUpdateInterval
        {
            get => _serverUpdateInterval;
            init => _serverUpdateInterval = Math.Max(value, 10);
        }
        
        public bool LimitServerCount { get; set; }
        public int MaxServerCount { get; set; }
        public string[]? ServersToDisplay { get; init; }
        
        public bool Debug { get; set; }
        public ConsoleExt.OutputType OutputMode { get; init; } = ConsoleExt.OutputType.None;
        public bool DryRun { get; init; }
        public bool AutoUpdate { get; init; }
    }
    
    public class ServerInfo
    {
        public int Id { get; init; }
        public string Uuid { get; init; } = null!;
        public string Name { get; init; } = null!;
        public EggInfo Egg { get; init; } = null!;
        public ServerResources? Resources { get; set; }
        public List<ServerAllocation>? Allocations { get; set; }
        public string? PlayerCountText { get; set; }
    }

    public record ServerResources
    {
        public string CurrentState { get; init; } = null!;
        public long MemoryBytes { get; init; }
        public long MemoryMaximum { get;  init; }
        public double CpuAbsolute { get; init; }
        public double CpuMaximum { get; init; }
        public long DiskBytes { get; init; }
        public long DiskMaximum { get; init; }
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
        public string MaxCpu { get; set; } = null!;
        public string Memory { get; set; } = null!;
        public string MaxMemory { get; set; } = null!;
        public string Disk { get; set; } = null!;
        public string MaxDisk { get; set; } = null!;
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