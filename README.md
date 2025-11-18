# Notifier

A small C# Discord bot that watches Roblox WindowsPlayer LIVE updates and posts status notifications to your server. It also helps announce your app status changes and exposes useful slash commands.

🔔 Minimal, focused, and easy to run.

## Features
- Detects Roblox updates via `clientsettings.roblox.com` and announces with rich embeds.
- Tracks version history from `setup.roblox.com/DeployHistory.txt` and keeps `saves/current` and `saves/previous` files.
- Public slash commands: `uptime`, `version`, `history`, `previous`, `help`.
- Admin commands: `sendupdate`, `print`, `getlogs`, `manualset`, `fetchhistory`, `refetch`.
- Optional Cloudflare WARP integration (`--warp`) to protect your IP while checking endpoints.

## Requirements
- `.NET SDK` matching the project target: `net10.0`.
- A Discord application and bot token with the proper intents.
- Optional: `warp-cli` installed if you want to use Cloudflare WARP.

## Setup
- Put your bot token into `auth.json` (file is copied to output on build):
  ```json
  {
    "token": "YOUR_BOT_TOKEN"
  }
  ```
- Set your Discord user ID in `Classes/Permissions.cs` so admin checks work:
  - Update `HostUserId` to your numeric Discord ID.
- Point the bot to your server by updating IDs in `Program.cs`:
  - Guild ID for command registration.
  - Text channel IDs for update notifications.
  - Voice channel IDs used for status/version naming.

## Run
- Restore and build:
  - `dotnet restore`
  - `dotnet build -c Release`
- Start the bot:
  - `dotnet run -- -w` to enable WARP (optional)
  - `dotnet run` to run normally

## Behavior
- On first run, creates a `saves` folder next to the executable containing:
  - `current` and `previous` version files.
  - `log.txt` with colorized console logs mirrored to disk.
- Registers slash commands to the configured guild and periodically rotates a simple status message.
- Posts embeds for Roblox updates and your app status with minimal emoji for clarity 🟢 🔴.

## Commands Overview
- Public:
  - `/uptime` – shows bot uptime.
  - `/version` – current Roblox WindowsPlayer (LIVE) version.
  - `/history` – latest 10 WindowsPlayer versions.
  - `/previous` – the previous version.
  - `/help` – lists commands.
- Admin:
  - `/sendupdate <message> <version|current> <unpatch|patch>` – post your app status.
  - `/print <message>` – log to console.
  - `/getlogs` – DM the current log file.
  - `/manualset <current|previous> <content>` – write saves.
  - `/fetchhistory` – refresh cached history.
  - `/refetch` – checks for changes manually without depending on the timer.

## Notes
- Keep your token private; do not commit real secrets.
- If your machine does not have `warp-cli`, run without `--warp`.
- Update IDs to match your server to avoid missing channel/guild errors.

## License
- No explicit license provided. Treat as private/internal unless stated otherwise.