# BallouBot Setup Guide

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later)
- A Discord account

---

## Step 1: Create a Discord Application

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Click **"New Application"** in the top-right corner
3. Give your application a name (e.g., "BallouBot") and click **"Create"**
4. You'll be taken to the application's General Information page

## Step 2: Create a Bot User

1. In the left sidebar, click **"Bot"**
2. You'll see the bot settings page. Your bot user is automatically created with new applications.
3. Under **"Token"**, click **"Reset Token"** to generate a new bot token
4. **Copy the token immediately** — you won't be able to see it again!
5. **⚠️ NEVER share your bot token or commit it to version control!**

## Step 3: Configure Gateway Intents

BallouBot requires specific Gateway Intents to function properly. On the Bot settings page:

1. Scroll down to **"Privileged Gateway Intents"**
2. Enable the following intents:
   - ✅ **Server Members Intent** — Required for detecting when users join (welcome messages)
   - ✅ **Message Content Intent** — Required for reading message content
3. Click **"Save Changes"**

## Step 4: Generate an Invite URL

1. In the left sidebar, click **"OAuth2"**
2. Under **"OAuth2 URL Generator"**, select the following scopes:
   - ✅ `bot`
   - ✅ `applications.commands`
3. Under **"Bot Permissions"**, select:
   - ✅ Send Messages
   - ✅ Send Messages in Threads
   - ✅ Embed Links
   - ✅ Read Message History
   - ✅ Use Slash Commands
   - ✅ View Channels
4. Copy the generated URL at the bottom
5. Open the URL in your browser to invite the bot to your server
6. Select the server you want to add the bot to and click **"Authorize"**

## Step 5: Configure the Bot Token

You have several options for configuring the bot token:

### Option A: appsettings.json (Development Only)

Edit `src/BallouBot.Host/appsettings.json`:

```json
{
  "Discord": {
    "Token": "YOUR_BOT_TOKEN_HERE"
  }
}
```

> ⚠️ **Do NOT commit this file with your real token.** Add it to `.gitignore`.

### Option B: User Secrets (Recommended for Development)

```bash
cd src/BallouBot.Host
dotnet user-secrets init
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN_HERE"
```

### Option C: Environment Variables (Recommended for Production)

Set the environment variable:

```bash
# Windows (PowerShell)
$env:Discord__Token = "YOUR_BOT_TOKEN_HERE"

# Windows (Command Prompt)
set Discord__Token=YOUR_BOT_TOKEN_HERE

# Linux/macOS
export Discord__Token="YOUR_BOT_TOKEN_HERE"
```

> Note: Use double underscores (`__`) as the hierarchy separator for environment variables.

## Step 6: Configure the Database

By default, BallouBot uses SQLite with a local file. The connection string is in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=balloubot.db"
  }
}
```

To use a different database (e.g., PostgreSQL), install the appropriate EF Core provider NuGet package and update the connection string and `Program.cs` configuration.

## Step 7: Run the Bot

```bash
cd src/BallouBot.Host
dotnet run
```

You should see output like:

```
[21:00:00 INF] Starting BallouBot...
[21:00:01 INF] Database migrations applied.
[21:00:02 INF] BallouBot connected to Discord.
[21:00:03 INF] Discord client is ready. Bot user: BallouBot (123456789)
[21:00:03 INF] Initialized module: Welcome Messages v1.0.0
[21:00:03 INF] All modules initialized. 1 module(s) loaded.
```

## Step 8: Test the Welcome Module

1. In your Discord server, type `/welcome channel #general` (or any text channel)
2. Use `/welcome message Welcome to {server}, {user}! You are member #{membercount}.`
3. Use `/welcome preview` to see how it looks
4. Use `/welcome toggle` to enable/disable
5. Use `/welcome embed` to switch to embed mode
6. Use `/welcome color FF5733` to change the embed color
7. Use `/welcome title Greetings!` to set the embed title

---

## Running Tests

```bash
# From the solution root
dotnet test
```

## Troubleshooting

### "Discord bot token is not configured"
Make sure you've set the token using one of the methods in Step 5.

### "Missing Access" or "Missing Permissions"
Make sure the bot has been invited with the correct permissions (Step 4) and that the bot role is positioned correctly in the server's role hierarchy.

### "Server Members Intent is required"
Make sure you've enabled the **Server Members Intent** in the Discord Developer Portal (Step 3).

### Bot doesn't respond to slash commands
Global slash commands can take up to an hour to propagate. Wait and try again, or restart the bot.
