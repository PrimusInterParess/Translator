# Translator (OversætMig)

Chrome/Edge translation helper extension (**`translator-ui/`**) with an optional local Text-to-Speech proxy (**`translator-proxy/`**) for higher-quality voices.

## What’s inside

- **`translator-ui/`**: MV3 browser extension (UI + content script + background service worker)
  - Translates via **MyMemory** (`https://api.mymemory.translated.net/`)
  - Pronunciation via:
    - **Browser/OS voice** (`chrome.tts`), or
    - **Local proxy** (recommended) → Google Cloud Text-to-Speech
- **`translator-proxy/`**: ASP.NET (net8.0) minimal API that exposes `POST /tts` on `http://127.0.0.1:8788`
- **`translator-ui/server/`**: optional Node/Express proxy (same idea as the .NET proxy)

For extension-only details (features, structure, debugging), also see `translator-ui/README.md`.

## Architecture

```mermaid
flowchart LR
  subgraph Browser["Chrome / Edge (MV3 extension)"]
    UI["Popup + Result UI (controls.html, result.html)"]
    CS["Content script (content.js)"]
    BG["Service worker (background.js)"]
    TTS["Browser/OS TTS (chrome.tts)"]
  end

  MyMemory["MyMemory Translate API (api.mymemory.translated.net)"]
  Proxy["Local TTS proxy (translator-proxy @ 127.0.0.1:8788)"]
  Google["Google Cloud Text-to-Speech API (texttospeech.googleapis.com)"]

  UI --> BG
  CS --> BG
  BG --> MyMemory
  BG --> TTS
  BG --> Proxy
  Proxy --> Google
```

## Quick start

### 1) Load the extension (unpacked)

1. Open extensions page:
   - Chrome: `chrome://extensions`
   - Edge: `edge://extensions`
2. Enable **Developer mode**
3. Click **Load unpacked**
4. Select folder: **`translator-ui/`** (the one that contains `manifest.json`)

### 2) (Recommended) Run the local TTS proxy

The extension’s **Pronunciation voice** setting defaults to “High quality voice (recommended)”, which expects a local proxy at `http://127.0.0.1:8788/tts`.

#### Option A — .NET proxy (recommended)

1. Put your Google API key into `translator-proxy/appsettings.Development.local.json` (this filename is already in `.gitignore`):

```json
{
  "GOOGLE_TTS_API_KEY": "YOUR_KEY_HERE"
}
```

2. Run:

```powershell
cd .\translator-proxy
dotnet run
```

Health check: `GET http://127.0.0.1:8788/health`

## Auto-start after reboot (Windows)

If you run the proxy via Docker, you can have it come back automatically after a restart:

1. Make sure **Docker Desktop** is set to **start on login**.
2. One-time: register a scheduled task (runs on your login and calls `start-translator.ps1`):

```powershell
.\scripts\register-startup-task.ps1
```

What this does:

- Creates/updates a Windows Scheduled Task named `Translator - start docker compose`
- Trigger: on your next login (with a small delay)
- Action: runs `.\scripts\start-translator.ps1`, which waits for Docker and then runs `docker compose up -d`
- Note: this is a **start/ensure-running** flow; it does **not** force recreation like `rebuild.cmd` (`--force-recreate`)
- The task is registered with **limited privileges** by default (no admin required). If you need highest privileges, run PowerShell as Administrator and use:

```powershell
.\scripts\register-startup-task.ps1 -RunLevel Highest
```

Verify the task exists:

```powershell
schtasks /Query /TN "Translator - start docker compose"
```

Logs go to: `%LOCALAPPDATA%\translator\logs\startup.log`

To remove the task later, run `.\scripts\unregister-startup-task.ps1`:

```powershell
.\scripts\unregister-startup-task.ps1
```

## Build a new Docker image on `git push` (local)

If you want a **local** “build on push” flow, you can use a Git `pre-push` hook. This runs **on your machine** right before the push is sent.

Required one-time setup (per repo clone): run `git config core.hooksPath .githooks` (hooks are local, so each machine/clone must do this once).

```powershell
git config core.hooksPath .githooks
```

Verify it’s enabled:

```powershell
git config --get core.hooksPath
```

Sanity check (make sure Git can see the hook file):

```powershell
type .\.githooks\pre-push
```

What happens:

- When you push `master` or `main`, it runs `rebuild.cmd`
- That script:
  - runs `docker compose build --pull`
  - recreates containers with `docker compose up -d --force-recreate` (brief local downtime)
  - cleans dangling images with `docker image prune -f`

How to confirm it ran:

- Run `git push origin master` (or `main`) and look for the `rebuild.cmd` output:
  - `=== Building images (pull latest base) ===`
  - `=== Recreating containers with new image ===`
  - `=== Cleaning dangling images ===`

Skip the hook for one push (if you’re in a hurry):

```powershell
git push --no-verify
```

Quick reference:

- How to verify it: `git config --get core.hooksPath`
- How to skip once: `git push --no-verify`

Optional publishing:

- If you want to publish images to a registry instead of (or in addition to) rebuilding the local compose stack, switch the hook to run `scripts/build-translator-proxy-image.ps1 -Push` and set `DOCKER_REGISTRY` / `TRANSLATOR_PROXY_IMAGE`.

#### Option B — Node proxy (alternative)

```powershell
cd .\translator-ui
npm install
$env:GOOGLE_TTS_API_KEY="YOUR_KEY_HERE"
npm run proxy
```

## Notes on secrets

- The proxy reads `GOOGLE_TTS_API_KEY` from configuration (local JSON in Development or environment variables).
- Keep keys in `appsettings.Development.local.json` (or env vars). Avoid committing keys into `appsettings.json`.

