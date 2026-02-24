# Translator (OversætMig)

Chrome/Edge translation helper extension (**`translator-ui/`**) with an optional local Text-to-Speech proxy (**`translator-proxy/`**) for higher-quality voices.

## What’s inside

- **`translator-ui/`**: MV3 browser extension (UI + content script + background service worker)
  - Translates via **MyMemory** (`https://api.mymemory.translated.net/`)
  - Pronunciation via:
    - **Browser/OS voice** (`chrome.tts`), or
    - **Local proxy** (recommended) → Google Cloud Text-to-Speech
- **`translator-proxy/`**: ASP.NET (net8.0) minimal API that exposes `POST /tts` on `http://127.0.0.1:8787`
- **`translator-ui/server/`**: optional Node/Express proxy (same idea as the .NET proxy)

For extension-only details (features, structure, debugging), also see `translator-ui/README.md`.

## Architecture

```mermaid
flowchart LR
  subgraph Browser["Chrome / Edge (MV3 extension)"]
    UI["Popup + Result UI (controls.html, result.html)"];
    CS["Content script (content.js)"];
    BG["Service worker (background.js)"];
    TTS["Browser/OS TTS (chrome.tts)"];
  end

  MyMemory["MyMemory Translate API (api.mymemory.translated.net)"];
  Proxy["Local TTS proxy (translator-proxy @ 127.0.0.1:8787)"];
  Google["Google Cloud Text-to-Speech API (texttospeech.googleapis.com)"];

  UI -->|runtime messages| BG;
  CS -->|selection + popup events| BG;
  BG -->|translate| MyMemory;
  BG -->|pronounce fallback| TTS;
  BG -->|pronounce recommended (POST /tts)| Proxy;
  Proxy -->|synthesize| Google;
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

The extension’s **Pronunciation voice** setting defaults to “High quality voice (recommended)”, which expects a local proxy at `http://127.0.0.1:8787/tts`.

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

Health check: `GET http://127.0.0.1:8787/health`

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

