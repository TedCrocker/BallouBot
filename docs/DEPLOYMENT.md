# BallouBot Deployment Guide

This guide walks you through deploying BallouBot to your Unraid server with automated deployments via GitHub Actions.

## Architecture Overview

```
Push to GitHub (main branch)
        ↓
GitHub Actions runs tests
        ↓
GitHub Actions builds Docker image
        ↓
Image pushed to GitHub Container Registry (ghcr.io)
        ↓
Watchtower on Unraid detects new image (polls every 5 min)
        ↓
Watchtower pulls new image & restarts BallouBot container
```

---

## Prerequisites

- A GitHub account with your BallouBot repo
- An Unraid server with Docker enabled
- Your Discord bot token (see [SETUP.md](SETUP.md))

---

## Step 1: Push Your Repo to GitHub

If you haven't already, create a GitHub repository and push your code:

```bash
git remote add origin https://github.com/YOUR_USERNAME/BallouBot.git
git push -u origin main
```

The GitHub Actions workflows (`.github/workflows/`) will automatically:
- **On every push to `master`**: Run tests, build Docker image, push to GHCR
- **On every PR**: Run tests only

### Verify the First Build

1. Go to your GitHub repo → **Actions** tab
2. You should see the "Deploy" workflow running
3. Once it completes, go to your repo's **Packages** section to confirm the Docker image was published

> **Note:** The first time, you may need to make the package visible. Go to your GitHub profile → **Packages** → click the `balloubot` package → **Package settings** → set visibility as needed.

---

## Step 2: Create a GitHub Personal Access Token (for Unraid)

Unraid needs to authenticate with GHCR to pull your image (even if your repo is public, it's good practice).

1. Go to [GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)](https://github.com/settings/tokens)
2. Click **"Generate new token (classic)"**
3. Give it a descriptive name: `unraid-ghcr-pull`
4. Select the scope: **`read:packages`** (that's all it needs)
5. Click **"Generate token"**
6. **Copy the token** — you'll need it in the next step

---

## Step 3: Set Up BallouBot on Unraid

### Option A: Using Unraid's Docker UI (Recommended)

1. Open the Unraid web UI
2. Go to **Docker** tab
3. At the bottom, toggle **Advanced View** on
4. Click **"Add Container"** and configure:

| Field | Value |
|---|---|
| **Name** | `balloubot` |
| **Repository** | `ghcr.io/YOUR_GITHUB_USERNAME/balloubot:latest` |
| **Registry URL** | `https://ghcr.io` |
| **Registry Username** | Your GitHub username |
| **Registry Password** | The personal access token from Step 2 |

5. Add the following **environment variables** (click "Add another Path, Port, Variable, Label, or Device" → select "Variable"):

| Name | Key | Value |
|---|---|---|
| Discord Token | `Discord__Token` | `YOUR_DISCORD_BOT_TOKEN` |
| Environment | `DOTNET_ENVIRONMENT` | `Production` |
| DB Connection | `ConnectionStrings__DefaultConnection` | `Data Source=/app/data/balloubot.db` |

6. Add the following **volume mappings** (Type: Path):

| Container Path | Host Path | Description |
|---|---|---|
| `/app/data` | `/mnt/user/appdata/balloubot/data` | SQLite database |
| `/app/logs` | `/mnt/user/appdata/balloubot/logs` | Log files |

7. Click **Apply**

### Option B: Using Docker Compose via SSH

1. SSH into your Unraid server
2. Create a directory for BallouBot:

```bash
mkdir -p /mnt/user/appdata/balloubot
cd /mnt/user/appdata/balloubot
```

3. Log in to GitHub Container Registry:

```bash
echo "YOUR_GITHUB_PAT" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

4. Create a `.env` file with your secrets:

```bash
cat > .env << 'EOF'
DISCORD_TOKEN=your_discord_bot_token_here
GITHUB_USER=your-github-username
EOF
```

5. Copy the `docker-compose.yml` from the repo (or create it):

```bash
curl -o docker-compose.yml https://raw.githubusercontent.com/YOUR_USERNAME/BallouBot/main/docker-compose.yml
```

6. Start the bot:

```bash
docker compose up -d
```

7. Check logs to verify it's running:

```bash
docker compose logs -f
```

---

## Step 4: Install Watchtower (Auto-Deploy)

Watchtower automatically detects when a new Docker image is available and updates your running container.

### Via Unraid Community Apps (Easiest)

1. Go to the **Apps** tab in Unraid
2. Search for **"Watchtower"**
3. Click **Install** on the `containrrr/watchtower` container
4. Configure the following:

| Field | Value |
|---|---|
| **WATCHTOWER_CLEANUP** | `true` (removes old images) |
| **WATCHTOWER_POLL_INTERVAL** | `300` (check every 5 minutes) |
| **WATCHTOWER_LABEL_ENABLE** | `false` (watch all containers, or `true` to only watch labeled ones) |

5. Click **Apply**

### Via Docker Compose (add to your compose file or run separately)

```bash
docker run -d \
  --name watchtower \
  --restart unless-stopped \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e WATCHTOWER_CLEANUP=true \
  -e WATCHTOWER_POLL_INTERVAL=300 \
  containrrr/watchtower
```

### Watch Only BallouBot (Optional)

If you don't want Watchtower updating all your containers, you can tell it to only watch specific ones:

```bash
docker run -d \
  --name watchtower \
  --restart unless-stopped \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e WATCHTOWER_CLEANUP=true \
  -e WATCHTOWER_POLL_INTERVAL=300 \
  containrrr/watchtower \
  balloubot
```

The last argument (`balloubot`) is the container name to watch. You can list multiple container names separated by spaces.

---

## Step 5: Verify the Pipeline

1. Make a small change to your bot code
2. Commit and push to `main`:

```bash
git add .
git commit -m "Test deployment pipeline"
git push
```

3. Watch the GitHub Actions tab — the workflow should:
   - ✅ Run tests
   - ✅ Build the Docker image
   - ✅ Push to `ghcr.io`

4. Within 5 minutes (Watchtower's poll interval), your Unraid container should automatically update

5. Verify on Unraid:

```bash
docker logs balloubot --tail 20
```

You should see the bot starting up with the latest changes.

---

## Troubleshooting

### Image won't pull on Unraid

- Make sure you're logged into GHCR: `docker login ghcr.io`
- Check the image name matches exactly: `ghcr.io/your-username/balloubot:latest`
- If your repo is private, ensure the PAT has `read:packages` scope

### Watchtower isn't updating

- Check Watchtower logs: `docker logs watchtower --tail 20`
- Make sure Watchtower can access the Docker socket
- Verify the image tag is `latest` (Watchtower watches for new digests on the same tag)
- If using GHCR with a private repo, Watchtower needs registry credentials:

```bash
docker run -d \
  --name watchtower \
  --restart unless-stopped \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v /root/.docker/config.json:/config.json:ro \
  -e WATCHTOWER_CLEANUP=true \
  -e WATCHTOWER_POLL_INTERVAL=300 \
  containrrr/watchtower \
  balloubot
```

(This mounts your Docker login credentials so Watchtower can authenticate with GHCR.)

### GitHub Actions build fails

- Check the Actions tab for error details
- Make sure `.NET 10 SDK` is available in the GitHub Actions runner (the workflow uses `dotnet-version: '10.0.x'`)
- Ensure all tests pass locally before pushing: `dotnet test`

### Database issues after update

The SQLite database is stored in a Docker volume (`/app/data`), so it persists across container updates. If you add new EF Core migrations, they should be applied automatically on startup.

---

## Useful Commands

```bash
# View bot logs
docker logs balloubot --tail 50 -f

# Restart the bot
docker restart balloubot

# Manually pull latest image
docker pull ghcr.io/YOUR_USERNAME/balloubot:latest

# Manually update (without Watchtower)
docker pull ghcr.io/YOUR_USERNAME/balloubot:latest
docker stop balloubot
docker rm balloubot
# Then recreate via Unraid UI or docker compose up -d

# Check Watchtower logs
docker logs watchtower --tail 20

# View database (from Unraid SSH)
ls -la /mnt/user/appdata/balloubot/data/

# View log files
ls -la /mnt/user/appdata/balloubot/logs/
```
