# Integration Testing Guide

This guide explains how to set up and run BallouBot's automated integration tests against a real Discord server.

---

## Overview

BallouBot uses a **hybrid integration testing** approach:

| Test Type | Database | Discord | Runs When |
|---|---|---|---|
| **Unit Tests** | In-memory | Mocked | Every push (CI) |
| **Hybrid Tests** | Real SQLite | Mocked events | Every push (CI) |
| **Discord Integration Tests** | Real SQLite | Real Discord API | Manual trigger |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Discord account
- A GitHub repository (for GitHub Actions CI)

---

## Step 1: Create a Test Discord Server

1. Open Discord and click the **"+"** button in the server list
2. Choose **"Create My Own"** → **"For me and my friends"**
3. Name it something like **"BallouBot Testing"**
4. Create a text channel called **`#test-welcome`** (or any name you prefer)

### Get the IDs

Enable **Developer Mode** in Discord:
1. Go to **Settings** → **Advanced** → enable **Developer Mode**

Now get the IDs:
1. **Guild (Server) ID**: Right-click the server name → **Copy Server ID**
2. **Channel ID**: Right-click the `#test-welcome` channel → **Copy Channel ID**

Save these — you'll need them for GitHub Secrets.

---

## Step 2: Create Two Bot Applications

You need **two** Discord bot applications:

### Bot 1: BallouBot (the bot under test)

If you haven't already created this, follow the [SETUP.md](SETUP.md) guide. Make sure:
- ✅ Server Members Intent is enabled
- ✅ Message Content Intent is enabled
- ✅ Bot is invited to the test server with all required permissions

### Bot 2: Tester Bot (sends commands and verifies responses)

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Click **"New Application"** → Name it **"BallouBot Tester"**
3. Go to **Bot** settings
4. Click **"Reset Token"** and copy the token
5. Enable these Gateway Intents:
   - ✅ **Server Members Intent**
   - ✅ **Message Content Intent**
6. Under **OAuth2**, generate an invite URL with:
   - Scopes: `bot`, `applications.commands`
   - Permissions: Send Messages, Read Message History, View Channels
7. Invite the tester bot to the **same test server**

### Verify Both Bots Are in the Server

After inviting both bots, you should see both in the server's member list:
- BallouBot (or whatever you named it)
- BallouBot Tester

---

## Step 3: Configure GitHub Secrets

In your GitHub repository, go to **Settings** → **Secrets and variables** → **Actions** → **New repository secret**:

| Secret Name | Value | Description |
|---|---|---|
| `TEST_BOT_TOKEN` | `MTIzNDU2Nz...` | BallouBot's bot token |
| `TESTER_BOT_TOKEN` | `OTg3NjU0Mz...` | Tester bot's token |
| `TEST_GUILD_ID` | `123456789012345678` | Test server ID |
| `TEST_CHANNEL_ID` | `987654321098765432` | Test channel ID |

> ⚠️ **Never** commit bot tokens to your repository!

---

## Step 4: Run Integration Tests

### Manually via GitHub Actions

1. Go to your repository on GitHub
2. Click **Actions** tab
3. In the left sidebar, click **"Integration Tests (Discord)"**
4. Click **"Run workflow"**
5. Optionally adjust the timeout (default: 20 seconds)
6. Click the green **"Run workflow"** button

The workflow will:
1. Build the solution
2. Start BallouBot connected to the test server
3. Wait for it to connect (configurable timeout)
4. Run the integration tests (tester bot connects and verifies)
5. Stop BallouBot and clean up

### Locally (for development)

Set the environment variables and run:

```bash
# Windows (PowerShell)
$env:TESTER_BOT_TOKEN = "your-tester-bot-token"
$env:TEST_GUILD_ID = "your-test-guild-id"
$env:TEST_CHANNEL_ID = "your-test-channel-id"

# Make sure BallouBot is running first (in another terminal):
# $env:Discord__Token = "your-balloubot-token"
# dotnet run --project src/BallouBot.Host

# Then run integration tests:
dotnet run --project tests/BallouBot.IntegrationTests
```

```bash
# Linux/macOS
export TESTER_BOT_TOKEN="your-tester-bot-token"
export TEST_GUILD_ID="your-test-guild-id"
export TEST_CHANNEL_ID="your-test-channel-id"

# In another terminal, start BallouBot:
# export Discord__Token="your-balloubot-token"
# dotnet run --project src/BallouBot.Host

# Run integration tests:
dotnet run --project tests/BallouBot.IntegrationTests
```

> **Note:** The hybrid tests (database + mocked Discord) will run even without the environment variables set. The Discord integration tests will be skipped if the environment variables are not configured.

---

## Test Categories

Tests are categorized so you can run specific sets:

| Category | Description | Needs Discord? |
|---|---|---|
| `Integration` | Real Discord API tests | ✅ Yes |
| `Hybrid` | Real SQLite + mocked Discord | ❌ No |

### Running Only Hybrid Tests (No Discord needed)

The hybrid tests always run because they don't require Discord credentials. They use real SQLite databases to verify the full data flow.

---

## What the Tests Verify

### Discord Connection Tests (`DiscordConnectionTests`)
- ✅ Tester bot can connect to Discord
- ✅ Tester bot can see the test server
- ✅ Tester bot can see the test channel
- ✅ BallouBot is present in the test server

### Slash Command Tests (`WelcomeSlashCommandTests`)
- ✅ `/welcome` command is registered
- ✅ All expected subcommands exist (channel, message, toggle, preview, embed, color, title)
- ✅ Command requires Manage Guild permission

### Hybrid Handler Tests (`WelcomeHandlerHybridTests`)
- ✅ Config reads from real SQLite database
- ✅ Embed message formatting works correctly
- ✅ Disabled config is respected
- ✅ GetOrCreateConfig works with real SQLite
- ✅ Config updates persist across database contexts

---

## Troubleshooting

### Tests skip with "environment variables not configured"
Set `TESTER_BOT_TOKEN`, `TEST_GUILD_ID`, and `TEST_CHANNEL_ID` before running.

### "Tester bot failed to connect within 30 seconds"
- Verify the `TESTER_BOT_TOKEN` is correct
- Check your internet connection
- Try increasing the timeout

### "BallouBot is not present in the test server"
Make sure BallouBot is running and connected to the test Discord server before running integration tests.

### Slash command tests fail with "welcomeCommand is null"
- BallouBot needs to be running and have registered its commands
- Global commands can take up to an hour to propagate. If running for the first time, wait or restart the bot.

### GitHub Actions workflow fails at "Verify secrets are configured"
One or more required secrets are missing. Go to **Settings** → **Secrets** in your GitHub repository and add all four secrets listed in Step 3.
