<p align="center">
  <img src="https://github.com/user-attachments/assets/3eb050aa-40c6-496f-94a3-8404987a6bf6" alt="Pelican Keeper" />
</p>

<h1 align="center">Pelican Keeper</h1>

<p align="center">
  A Discord bot for monitoring and displaying the status of your
  <a href="https://pelican.dev/">Pelican Game Servers</a>.
</p>

<p align="center">
  <strong>Self-hosted • Lightweight • Customizable</strong>
</p>

<p align="center">
  <a href="https://ko-fi.com/sirzeeno" target="_blank">
    <img src="https://ko-fi.com/img/githubbutton_sm.svg" alt="Support me on Ko-fi" />
  </a>
</p>

---

## ✨ Features

Pelican Keeper connects to your Pelican panel and displays live server information directly in Discord.

| Feature                     | Supported  |
|-----------------------------|:----------:|
| 🖥️ CPU Usage                |     ✅     |
| 🧠 Memory Usage             |     ✅     |
| 💾 Disk Usage               |     ✅     |
| 🌐 Network RX/TX            |     ✅     |
| ⏱️ Server Uptime             |     ✅     |
| 👥 Player Count             |     ✅     |
| 🎮 Server Join IP & Port    |     ✅     |
| 📊 Consolidated Server View |     ✅     |
| 📖 Pagination               |     ✅     |
| ✏️ Custom Message Templates |     ✅     |
| 🔄 Automatic Updates        |     ✅     |
| 🥚 Pelican Egg              |     ✅     |
| 🐳 Docker Container         | 🚧 Planned |

---

## 🚀 Installation

There are three ways to run Pelican Keeper.

| Method                 | Recommended | Best For                        |
|------------------------|:-----------:|---------------------------------|
| **Pelican Keeper Egg** | ⭐ **Yes**  | Running directly inside Pelican |
| **Generic C# Egg**     |             | Existing Pelican installations  |
| **Standalone Release** |             | Running outside Pelican         |

### 🥚 Pelican Keeper Egg — Recommended

The easiest way to run Pelican Keeper is using the provided Pelican Egg.

#### 1. Download the Egg

Download the latest Egg from the repository and import it into your Pelican panel.

#### 2. Create the Server

Create a new server using the Pelican Keeper Egg.

The Egg exposes the bot's configuration options directly through Pelican.

> **Note:** Configuration changes require a bot restart to take effect.

#### 3. Start the Bot

Start the server once.

Pelican Keeper will automatically create the required configuration files, including:

```text
Config.json
Secrets.json
MessageHistory.json
```

#### 4. Configure `Secrets.json`

Open the **Files** tab and edit `Secrets.json`.

You will need to provide your Pelican API credentials and Discord bot information.

See the [Bot Secrets Wiki](https://github.com/SirZeeno/Pelican-Keeper/wiki/Bot-Secrets) for details.

---

## 🧩 Generic C# Egg

If you prefer to use Pelican's Generic C# Egg, configure it with the following values:

```text
Git Repo Address: https://github.com/SirZeeno/Pelican-Keeper
Install Branch: main
Project Location: /home/container/Pelican Keeper/
Project File: Pelican Keeper
```

After installation, start the bot once to generate `Secrets.json`, or create the file manually in the server's base directory.

Then fill in the required credentials.

---

## 💻 Standalone Installation

Pelican Keeper can also run outside of Pelican on any supported platform.

### Requirements

* `curl`
* `unzip`
* .NET 8 runtime

### Download the Latest Release

Replace `[Your_Platform_Version]` with the appropriate release for your platform:

```bash
curl -L https://github.com/SirZeeno/Pelican-Keeper/releases/latest/download/[Your_Platform_Version].zip \
  -o Pelican-Keeper.zip

unzip Pelican-Keeper.zip
rm Pelican-Keeper.zip
```

### Configure the Bot

Create `Secrets.json` in the bot's installation directory.

If it does not exist, Pelican Keeper will generate a default file on first startup.

See the [Bot Secrets Wiki](https://github.com/SirZeeno/Pelican-Keeper/wiki/Bot-Secrets) for the required values.

### Start the Bot

From the installation directory:

```bash
dotnet run --project "Pelican Keeper"
```

---

# ⚙️ Configuration

Pelican Keeper uses two primary configuration files:

| File                  | Purpose                                                  |
| --------------------- | -------------------------------------------------------- |
| `Secrets.json`        | API credentials and Discord connection information       |
| `Config.json`         | Bot behavior, filtering, formatting, and display options |
| `MessageMarkdown.txt` | Discord message template                                 |

> **Important:** Changes to `Config.json` require a bot restart before they take effect.

### 🔐 Secrets

`Secrets.json` contains the credentials required for Pelican Keeper to communicate with your Pelican panel and Discord.

You'll need:

* Pelican **Client API Token**
* Discord **Bot Token**
* Discord **Channel ID**
* Other optional connection settings

See the complete [Bot Secrets documentation](https://github.com/SirZeeno/Pelican-Keeper/wiki/Bot-Secrets).

> ⚠️ **Never share your `Secrets.json` or commit it to Git.**

---

## 🛠️ Display Modes

Pelican Keeper supports three ways of displaying your servers.

### 📦 Consolidated

All servers are displayed together in a single Discord message.
Discord embeds support a maximum of **25 fields**, so larger server installations may need to use pagination.

![Consolidated Server View](https://github.com/user-attachments/assets/9ec54b8d-48fa-424c-acd3-5bb12222f2ef)

---

### 📖 Paginated

Display servers one at a time in a single Discord message with navigation controls.

![Paginated Server View](https://github.com/user-attachments/assets/7cb58936-71f7-4378-9256-0a79c5056256)

> **Note:** Pagination is shared across the message. If one user changes the page, the displayed page changes for everyone viewing that message.

---

### 💬 Per-Server Messages

Each server gets its own Discord message.

This can make larger server lists easier to read, but it also generates more Discord API traffic and may run into Discord rate limits.

---

# ✏️ Custom Message Formatting

Pelican Keeper supports customizable Discord message templates through:

```text
MessageMarkdown.txt
```

The template uses Discord-style Markdown with a few Pelican Keeper-specific additions.

See the [MessageMarkdown.txt](https://github.com/SirZeeno/Pelican-Keeper/blob/main/Pelican%20Keeper/MessageMarkdown.txt) file for the default template.

### Variables

Variables are written using:

```text
{{VariableName}}
```

and are automatically replaced with the corresponding server information.

| Variable          | Description              |
| ----------------- | ------------------------ |
| `{{Uuid}}`        | Server UUID              |
| `{{ServerName}}`  | Server name              |
| `{{StatusIcon}}`  | Status-dependent icon    |
| `{{Status}}`      | Current server status    |
| `{{Cpu}}`         | CPU usage                |
| `{{Memory}}`      | Memory usage             |
| `{{Disk}}`        | Disk usage               |
| `{{NetworkRx}}`   | Incoming network traffic |
| `{{NetworkTx}}`   | Outgoing network traffic |
| `{{Uptime}}`      | Server uptime            |
| `{{PlayerCount}}` | Current player count     |

### Server Title

The special `[Title]` tag identifies the server name used for the Discord embed title.

Example:

```text
[Title]
{{StatusIcon}} {{ServerName}}
[/Title]
```

---

# 🔧 Configuration Reference

For a complete explanation of every configuration option, see:

**[📖 Bot Configuration Wiki](https://github.com/SirZeeno/Pelican-Keeper/wiki/Bot-Config)**

For API credentials and secrets:

**[🔐 Bot Secrets Wiki](https://github.com/SirZeeno/Pelican-Keeper/wiki/Bot-Secrets)**

---

# 📁 Project Structure

A typical installation looks like:

```text
Pelican Keeper/
├── Config.json
├── Secrets.json
├── MessageHistory.json
├── MessageMarkdown.txt
└── Pelican Keeper
```

`Secrets.json` contains sensitive information and should **never** be shared publicly.

---

# 🐳 Docker

Docker support is planned but is not currently available.

| Deployment     |    Status   |
| -------------- | :---------: |
| Pelican Egg    | ✅ Supported |
| Generic C# Egg | ✅ Supported |
| Standalone     | ✅ Supported |
| Docker         |  🚧 Planned |

---

# 🤝 Contributing

Contributions, bug reports, and feature requests are welcome.

If you find a problem or have an idea for improving Pelican Keeper, open an issue or pull request on GitHub.

---

# 💖 Support

If Pelican Keeper is useful to you and you'd like to support development:

<p align="center">
  <a href="https://ko-fi.com/sirzeeno" target="_blank">
    <img src="https://ko-fi.com/img/githubbutton_sm.svg" alt="Support me on Ko-fi" />
  </a>
</p>

---

<p align="center">
  Made for <a href="https://pelican.dev/">Pelican</a> ❤️
</p>
