# Pelican Keeper - AI Agent Instructions

## Project Overview

Discord bot for monitoring Pelican Panel game servers (.NET 8.0, C#). Displays real-time server stats (CPU, memory, network, uptime, player counts) in Discord embeds that update automatically.

## Architecture

### Project Structure

```
Pelican-Keeper/
├── Pelican Keeper/           # Main application
│   ├── Core/                 # RuntimeContext, RepoConfig
│   ├── Models/               # Config, Secrets, ServerInfo, Enums, ViewModels
│   ├── Configuration/        # FileManager, Validator
│   ├── Discord/              # EmbedBuilderService, InteractionHandler, LiveMessageStorage
│   ├── Pelican/              # PelicanApiClient, ServerMonitorService, JsonResponseParser
│   ├── Query/                # IQueryService implementations (A2S, RCON, Minecraft, Terraria)
│   ├── Utilities/            # Logger, FormatHelper, NetworkHelper
│   └── Updates/              # VersionUpdater
├── Tests/                    # NUnit unit tests
├── deploy/                   # Deployment config files
├── build.sh                  # Local build script
└── .github/workflows/        # CI/CD pipeline
```

### Core Components

| File | Purpose |
|------|---------|
| [Program.cs](Pelican%20Keeper/Program.cs) | Main entry point, Discord client setup, update loop |
| [Core/RuntimeContext.cs](Pelican%20Keeper/Core/AppContext.cs) | Global state (Config, Secrets, TargetChannels, ServerInfo) |
| [Core/RepoConfig.cs](Pelican%20Keeper/Core/RepoConfig.cs) | GitHub repository configuration constants |
| [Pelican/PelicanApiClient.cs](Pelican%20Keeper/Pelican/PelicanApiClient.cs) | REST API calls to Pelican Panel |
| [Pelican/ServerMonitorService.cs](Pelican%20Keeper/Pelican/ServerMonitorService.cs) | Server filtering, player counts, shutdown logic |
| [Discord/EmbedBuilderService.cs](Pelican%20Keeper/Discord/EmbedBuilderService.cs) | Discord embed construction |
| [Discord/InteractionHandler.cs](Pelican%20Keeper/Discord/InteractionHandler.cs) | Button/dropdown event handlers |
| [Discord/LiveMessageStorage.cs](Pelican%20Keeper/Discord/LiveMessageStorage.cs) | Persists Discord message IDs |
| [Discord/ServerMarkdownParser.cs](Pelican%20Keeper/Discord/ServerMarkdownParser.cs) | Template parsing with `{{placeholder}}` syntax |

### Configuration Files

| File | Purpose |
|------|---------|
| `Secrets.json` | API tokens, Discord bot token, channel IDs |
| `Config.json` | Bot behavior, display options, auto-shutdown rules |
| `GamesToMonitor.json` | Maps Pelican egg IDs to query protocols |
| `MessageMarkdown.txt` | Custom template with `[Title]`, `[Body]` tags |
| `MessageHistory.json` | Tracks sent Discord message IDs for editing |

### Key Patterns

**Static Context**: `RuntimeContext` holds global state
- `RuntimeContext.Config` - Bot configuration
- `RuntimeContext.Secrets` - API credentials  
- `RuntimeContext.TargetChannels` - Discord channels to post to
- `RuntimeContext.GlobalServerInfo` - Current server data

**Template System**: `ServerMarkdownParser` uses regex to replace `{{Property}}` placeholders

**Query Service Interface**:
```csharp
public interface IQueryService
{
    Task<string?> QueryPlayerCountAsync(string ip, ushort port);
}
```

**File Manager**: `FileManager.GetFilePath()` recursively searches, returns empty string on failure

## Build & Test

### Local Build

```bash
./build.sh                    # Current platform, release
./build.sh linux|windows|osx  # Specific platform
./build.sh all                # All platforms
./build.sh linux debug        # Debug configuration
```

### Running Tests

```bash
dotnet test "Pelican Keeper.sln"
```

Tests use `TestContext.CurrentContext.TestDirectory` to find config files.

## Common Tasks

### Adding New Game Query Support

1. Create service in `Query/` implementing `IQueryService`
2. Add enum value to `QueryMethod` in `Models/Enums.cs`
3. Update `GamesToMonitor.json` with egg ID mapping
4. Register in `ServerMonitorService` switch statement

### Modifying Display Format

1. Edit `MessageMarkdown.txt` template
2. Add properties to `Models/ServerViewModel.cs`
3. Update `ServerMarkdownParser.ParseTemplate()` to populate properties

### Adding Configuration Options

1. Add property to `Models/Config.cs`
2. Update `Configuration/Validator.cs` if validation needed
3. Document in README.md

## Dependencies

- `DSharpPlus` 4.5.1 - Discord API
- `RestSharp` 112.1.0 - HTTP client
- `Newtonsoft.Json` - JSON serialization
- `NUnit` 4.2.2 - Testing
