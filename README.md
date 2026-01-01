<p align="center">
  <img src="https://github.com/user-attachments/assets/3eb050aa-40c6-496f-94a3-8404987a6bf6"/><br>
    <strong>Support me and my projects</strong><br>
  <a href="https://ko-fi.com/sirzeeno" target="_blank">
    <img src="https://ko-fi.com/img/githubbutton_sm.svg" alt="Support me on Ko-fi" />
  </a>
</p>

# Pelican Keeper

A Discord bot that will display the status of your Pelican Game Servers in a Discord Channel

> [!CAUTION]
> The Secrets File has been changed to accommodate multichannel messaging, Look at the JSON structure below where you'll see the new structure!

This Discord bot is a basic compile-and-run bot built in .NET 8.0.
> [!TIP]
> This bot can be installed and run on the generic C# Egg on Pelican.

| Feature                 | Description                                                                 | Supported   |
|-------------------------|-----------------------------------------------------------------------------|-------------|
| CPU Usage               | Shows live CPU percentage                                                   | ✅           |
| Memory Usage            | Displays RAM used by the server                                             | ✅           |
| Disk Space              | Shows total disk usage                                                      | ✅           |
| Network Stats           | RX/TX bandwidth tracking                                                    | ✅           |
| Uptime                  | Displays how long the server has been running                               | ✅           |
| Per Server Messages     | Puts each server in its own message (Subject to rate limitation)            | ✅           |
| Consolidation           | Consolidates All Servers into one message (limited to 25 in a single embed) | ✅           |
| Pagination              | Flip through multiple servers in one paginated message                      | ✅           |
| Custom Templates        | Markdown style message embed system for customizable structure              | ✅           |
| Auto Updates            | Messages update automatically every X seconds                               | ✅           |
| Player Count            | Show live player count from server (if supported)                           | ✅ |
| Server Joinable IP:Port | Show server joinable IP:Port                                                | ✅ |
| Pelican Egg             | Installable Egg where you can run and configure the bot in the panel        | ❌ (Planned) |

## Setup in Pelican

### Setup Egg

Set up the Generic C# egg in your Pelican Panel like normal.

### Configuration

Set the following variables exactly like this

```
- Git Repo Address: https://github.com/SirZeeno/Pelican-Keeper
- Install Branch: main
- Project Location: /home/container/Pelican Keeper/
- Project File: "Pelican Keeper"
```

### Secrets

Run the bot once or create the Secrets.json in the base directory you see when opening the Files tab and fill out all the necessary information.

## Setup Outside Pelican
>
> [!TIP]
> You will need curl installed.

### Get The Latest Release

Download the latest release with this command. This command downloads the latest release for your specific platform, unzips it, and removes the zip file.

```
curl -L https://github.com/SirZeeno/Pelican-Keeper/releases/latest/download/[Your_Platform_Version].zip && unzip [Your_Platform_Version].zip && rm [Your_Platform_Version].zip
```

### Run the Bot
>
> [!WARNING]
> At this point you should create the Secrets.json in the directory the bot resides in, otherwise the first run will result in an error.<br>
> If you don't create the Secrets.json, the bot will create a default one for you that you will need to fill out.<br>
> After the first run, the bot will have created the Secrets.json. At that point it's your responsibility to fill out with all the necessary information.

while still in the same directory to start the bot, you simply run this command

```
dotnet run --project "Pelican Keeper"
```

## Secrets

> [!TIP]
> The Server and Client Tokens are API keys you generate from your Pelican panel.
> The Admin page is where you can generate the server API token, and the client API token is under the Profile settings.

> [!WARNING]
> Make sure you fill out the Secrets.json file found in the bot install directory, otherwise the bot **WILL NOT** work!

The Secrets file will be created on first startup if it does not exist. The structure of the file and what each Variable is for can be found in the [Wiki](https://github.com/SirZeeno/Pelican-Keeper/wiki/Bot-Secrets)

## Configuration

### Config File

> [!TIP]
> This file is used to configure how the bot behaves and displays information in Discord.
> You can edit this file to change the bot's behavior, changing it while the bot is running will not apply the changes until the next restart.

The config.json file can be found in the installation folder, and the structure and what each setting does is explained
in the [Wiki](https://github.com/SirZeeno/Pelican-Keeper/wiki/Bot-Config)

### ConsolidateEmbeds

> Consolidate all the server information into a single embed message if true. And if false, it will create an embed message for each game server you have on pelican.
>
> Note that you will run into discord rate limits if you have this option off, and you have a few game servers on pelican.

![image](https://github.com/user-attachments/assets/9ec54b8d-48fa-424c-acd3-5bb12222f2ef)

### Paginate

> Paginates the server information into a single embed message with changeable pages.
>
> Note that the page will be the same serverwide, meaning if someone changed the page, it changes it for you as well.

![image](https://github.com/user-attachments/assets/7cb58936-71f7-4378-9256-0a79c5056256)

### Markdown Formatting
>
> [!TIP]
> The Markdown file can be found in [here](https://github.com/SirZeeno/Pelican-Keeper/blob/main/Pelican%20Keeper/MessageMarkdown.txt).

The Markdown formatting uses [Discord's](https://support.discord.com/hc/en-us/articles/210298617-Markdown-Text-101-Chat-Formatting-Bold-Italic-Underline) formatting with some slight modifications.<br>
Anything marked with {{}} will be replaced with the variable name.<br>
Finally, There is a special Tag called [Title] for the server Name to mark what is supposed to be the server name for special use in the title of the embed.

| Variable   | Description                                      |
|------------|--------------------------------------------------|
| Uuid       | UUID of the server                               |
| ServerName | Name of the server                               |
| StatusIcon | Icon that changes depending on the server status |
| Status     | Status of the server (Offline/Starting/Running)  |
| Cpu        | CPU usage of the server                          |
| Memory     | Memory usage of the server                       |
| Disk       | Disk space used by the server                    |
| NetworkRx  | Inbound network traffic                          |
| NetworkTx  | Outbound network traffic                         |
| Uptime     | Uptime of the server                             |

## Development

### Project Structure

```
Pelican-Keeper/
├── Pelican Keeper/          # Main application
│   ├── Core/                # App context and configuration
│   ├── Models/              # Data models (Config, Secrets, ServerInfo, etc.)
│   ├── Configuration/       # File management and validation
│   ├── Discord/             # Discord embeds, interactions, markdown parser
│   ├── Pelican/             # Pelican API client and server monitoring
│   ├── Query/               # Game server query services (A2S, RCON, Minecraft, etc.)
│   ├── Utilities/           # Helpers (formatting, logging, network)
│   └── Updates/             # Version checking and auto-updates
├── Tests/                   # Unit tests
├── deploy/                  # Deployment files
├── build.sh                 # Local build script
└── .github/workflows/       # CI/CD pipeline
```

### Building Locally

Use the included build script for easy local builds:

```bash
# Build for current platform (release)
./build.sh

# Build for specific platform
./build.sh linux          # Linux x64
./build.sh windows        # Windows x64
./build.sh osx            # macOS x64
./build.sh all            # All platforms

# Build with debug configuration
./build.sh linux debug
```

Output will be in the `dist/` directory.

### Manual Build

```bash
# Debug build
dotnet build "Pelican Keeper.sln"

# Release build (single-file executable)
dotnet publish "Pelican Keeper/Pelican Keeper.csproj" -c Release -r linux-x64 --self-contained
```

### Running Tests

```bash
dotnet test "Pelican Keeper.sln"
```

Tests require config files (`Config.json`, `GamesToMonitor.json`, `MessageMarkdown.txt`) in the test output directory. These are automatically copied from the main project during build.
