# OversætMig (Chrome/Edge extension)

Small translation helper that translates selected text using the **MyMemory** API.

## Features

- Translate highlighted text via **right click → “OversætMig: Translate selection”**
- On-page selection popup:
  - Appears when you keep the same selection for ~2 seconds
  - Click elsewhere to close
  - Can be disabled from the extension popup (**Extension enabled** toggle)
- Pronunciation (text-to-speech):
  - On-page popup has a **speaker icon** button to listen
  - Result window has the same **speaker icon** button to listen
- Popup settings:
  - **Email** (optional) to increase MyMemory quota
  - **Translate from** (including `auto` guess; MyMemory itself does not auto-detect)
  - **Translate to**
- Pronunciation settings:
  - **Pronunciation speed**
  - **Pronunciation volume**
- Result window with **Translate** button and **Ctrl+Enter** shortcut

## Install (unpacked)

1. Open your browser extensions page:
   - Chrome: `chrome://extensions`
   - Edge: `edge://extensions`
2. Enable **Developer mode**
3. Click **Load unpacked**
4. Select this project folder: `translator`

## How to use

- **From any page (context menu)**: select text → right click → **OversætMig: Translate selection**
  - A small result window opens and shows the translation.
- **From any page (on-page popup)**: select text and keep the same selection for ~2 seconds
  - A small popup appears near the cursor.
- **From the extension popup** (toolbar icon):
  - Set **Email**, **Translate from**, **Translate to**
  - Adjust **Pronunciation speed** and **Pronunciation volume** if needed
- **In the result window**:
  - Edit the text if needed and click **Translate** (or press **Ctrl+Enter**)
  - Click the **speaker icon** button to listen to pronunciation

## Limits / notes

- Max text length is **500 characters** (longer selections are ignored / rejected).
- Network calls go only to `https://api.mymemory.translated.net/`.
- MyMemory does not auto-detect language; `auto` in the UI means **the extension guesses** a source language.

## Development

### Prerequisites

- Node.js (recommended: modern LTS)

### Install dependencies

```bash
npm install
```

### Useful scripts

```bash
npm run lint
npm run format
npm test
```

### Reload after changes

- After changing extension code: go to `chrome://extensions` / `edge://extensions` → **Reload**
- After changing content scripts: reload the extension and **refresh the page**

### Debugging (MV3 service worker)

- `chrome://extensions` / `edge://extensions` → your extension → **Service worker** (inspect)
- Service worker can stop/start automatically; open the inspector to see logs.

## Permissions / privacy

- `storage`: saves settings + translations state
- `contextMenus`: adds the “Translate selection” menu item
- `tts`: plays pronunciation using the browser/OS text-to-speech engine
- Host permission: `https://api.mymemory.translated.net/*`

## Project structure (high level)

### Entry points (referenced by `manifest.json` / HTML files)

- `manifest.json`: extension configuration (MV3)
- `background.js`: MV3 service worker entrypoint (loads modules via `importScripts(...)`)
- `content.js`: content script entrypoint (on-page selection watcher + popup trigger)
- `controls.html` / `popup.js`: extension popup UI
- `result.html` / `result.js`: translation result window UI

### Main folders

- `background/*`: background handlers, result window helper, MyMemory client
- `content/*`: on-page popup implementation
- `core/*`: translation + TTS services used by background handlers
- `shared/*`: shared helpers (OM namespace, storage/settings, runtime messaging, lang guessing, text sanitizing, UI helpers)
- `tests/*`: Node test runner tests

