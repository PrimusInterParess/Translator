# How `translator-ui/` works (step-by-step)

`translator-ui/` is a **Chrome/Edge extension (Manifest V3)** written in vanilla JavaScript. The most important idea is:

- There are **multiple execution environments** (background, content script, popup UI, result window).
- They communicate using **messages** (`chrome.runtime.sendMessage`) and **storage** (`chrome.storage.local`).
- Most modules are attached under one global namespace object: **`globalThis.OM`**.

## 1) The 4 places your code runs

### 1.1 Background service worker (extension “backend”)

- **File**: `background.js` (registered by `manifest.json`)
- **Purpose**: owns privileged extension APIs:
  - context menu item
  - message handler (`onMessage`)
  - opening a separate result window
  - calling translation/TTS services (via your local proxy)

### 1.2 Content scripts (runs inside every webpage)

- **Files**: `content.js`, `content/popup.js` (plus many `shared/*.js`)
- **Purpose**:
  - detect selection on the page
  - after a “hold delay”, show a small on-page popup near the cursor
  - ask the background to translate / speak via messages

### 1.3 Popup UI (the extension popup when you click the icon)

- **Files**: `controls.html`, `popup/controller.js`, `popup.js`
- **Purpose**:
  - settings UI (enable/disable, languages, delays, TTS preferences)
  - LLM features (verb forms, explain) — direct `fetch` to the proxy

### 1.4 Result window (separate popup window)

- **Files**: `result.html`, `result/controller.js`, `result.js`
- **Purpose**:
  - show a larger translation UI (textarea + Translate + Speak)
  - auto-update when the background writes translation progress/results to storage

## 2) How modules are structured (the `OM` namespace pattern)

Most files look like this:

- Wrap everything in an IIFE: `(() => { ... })();`
- Grab the shared namespace: `const OM = (globalThis.OM = globalThis.OM || {});`
- Export exactly one module by assigning to `OM.something = Object.freeze(...)`.

The shared namespace is created in:

- **File**: `shared/om.js`

## 3) The wiring: `manifest.json`

**File**: `manifest.json`

Key parts:

- `background.service_worker = "background.js"`
- `action.default_popup = "controls.html"`
- `content_scripts` inject a bundle of scripts into **`<all_urls>`** pages:
  - many `shared/*.js`
  - `content/popup.js`
  - `content.js`
  - plus `style.css`

Also important:

- `host_permissions`: allows calls to your local proxy:
  - `http://127.0.0.1/*`
  - `http://localhost/*`

## 4) Shared building blocks (learn these once)

### 4.1 Constants

- **File**: `shared/constants.js`
- **Important constants**:
  - `MAX_TEXT_LEN = 500`
  - default proxy URLs:
    - translate: `http://127.0.0.1:8788/translate/mymemory`
    - verb forms: `http://127.0.0.1:8788/verbforms`
    - explain: `http://127.0.0.1:8788/explain`
    - TTS: `http://127.0.0.1:8788/tts`

### 4.2 Storage wrapper

- **Files**: `shared/storage.js`, `shared/chromeStorageRepo.js`
- `OM.storage.get/set/remove(...)` wraps `chrome.storage.local` using Promises.

### 4.3 Settings wrapper

- **File**: `shared/settings.js`
- `OM.settings.get()` reads storage keys and normalizes values (clamps sliders etc).
- `OM.settings.setEnabled/setSourceLang/...` writes a single key to storage.

### 4.4 Messaging wrapper

- **File**: `shared/runtime.js`
- `OM.runtime.sendMessage(msg)` wraps `chrome.runtime.sendMessage` and always resolves to an object like:
  - `{ ok: true, ... }` or `{ ok: false, error: "..." }`

Message “types” are defined in:

- **File**: `shared/messages.js`

### 4.5 Translation record storage (“database”)

- **File**: `shared/translationsRepo.js`
- Stores each translation record under a key like:
  - `translation:<id>`
- Also stores:
  - `lastTranslationId`
  - `translationHistory` (up to 20 IDs)

## 5) Main user story: select text on a webpage

### Step A — detect selection + wait for hold delay

- **File**: `content.js`

Flow:

- Read settings (especially `enabled` and `holdToTranslateMs`).
- On `mouseup`:
  - read selected text (`window.getSelection().toString()`)
  - sanitize and length-check
  - start a timer
- If the selection stays the same for `holdToTranslateMs`, open the on-page popup:

- Calls:
  - `OM.contentPopup.openAt({ x, y, text })`

### Step B — create the on-page popup DOM

- **File**: `content/popup.js`

It builds a small UI into the webpage:

- creates `<div id="trans-popup">` at `(x, y)`
- adds:
  - close button
  - textarea input (pre-filled with selected text)
  - output area
  - speak button
- immediately starts translation

### Step C — translation is done by messaging the background

Still in `content/popup.js`, translation is:

- `OM.runtime.sendMessage({ type: "translate", text, guessedSource })`

This jumps from content script → background service worker.

### Step D — background handles the request

- **File**: `background/main.js`
- It registers `chrome.runtime.onMessage` and forwards to:
  - `OM.backgroundHandlers.onMessage(...)`

- **File**: `background/handlers.js`
- For message type `"translate"` it:
  - creates a `requestId` (or uses the one provided)
  - writes `{ status: "translating" }` to `translationsRepo` (storage)
  - calls `OM.translatorService.translateFromMessage(...)`
  - writes `{ status: "done", translatedText, source, target }` (or `{ status: "error", ... }`) back to storage
  - returns a small response to the caller (content popup / result window)

### Step E — translator service calls your local proxy

- **File**: `core/translatorService.js`
- Validates:
  - enabled not paused
  - text present and <= 500 chars
  - chooses source/target languages (auto-detect if needed)
- Calls:
  - `OM.mymemory.translate(...)`

- **File**: `background/mymemory.js`
- Does a `fetch` to:
  - `http://127.0.0.1:8788/translate/mymemory`

So: **the extension expects your local `translator-proxy` to be running on port 8788**.

## 6) Context menu story: “Translate selection”

When you right-click selected text and click the context menu:

- **File**: `background/handlers.js` → `onContextMenuClick(...)`
- It:
  - creates a `requestId`
  - writes a “translating” record to storage
  - opens the result window immediately:
    - `OM.resultWindow.open(requestId)` (in `background/resultWindow.js`)
  - performs translation
  - updates storage record with done/error

## 7) Why the result window updates live

- **File**: `result/controller.js`

Key idea:

- The result window reads `?id=<requestId>` from its URL.
- It loads the initial record from storage:
  - `OM.translationsRepo.get(requestId)`
- It subscribes to:
  - `chrome.storage.onChanged`
- When the specific key `translation:<requestId>` changes, it re-renders the UI.

So the result window behaves like “live UI” but it’s implemented with **storage as the event bus**.

## 8) Popup UI (settings + Gemini features)

### 8.1 Settings

- **Files**: `controls.html`, `popup/controller.js`

Flow:

- `popup/controller.js` loads settings via `OM.settings.get()`
- It fills the DOM inputs with initial values
- It registers event listeners:
  - on toggle/range/select change → calls `OM.settings.setX(...)`
- Content scripts and background listen to `chrome.storage.onChanged`, so changes apply quickly.

### 8.2 LLM features (verb forms + explain)

These are **not** done via background messages. The popup calls the proxy directly (`fetch` to URLs in `shared/constants.js`):

- Verb forms: `C.DEFAULTS.verbFormsProxyUrl` → `http://127.0.0.1:8788/verbforms`
- Explain: `C.DEFAULTS.explainProxyUrl` → `http://127.0.0.1:8788/explain`

The proxy uses **Ollama** (default) or **Gemini** depending on `Llm:Provider` — the route names do not imply Gemini only.

- Verb forms UI/rendering: `popup/controller.js`
- Explain structured rendering: `popup/explain-render.js`
- Standalone features window: `features.html` + `features.js` (reuses `popup/controller.js`)

## 9) Text-to-speech (TTS)

### 9.1 UI sends messages

Both `content/popup.js` and `result/controller.js` do:

- `OM.runtime.sendMessage({ type: "ttsInfo" })`
- `OM.runtime.sendMessage({ type: "ttsSpeak", text, lang })`
- `OM.runtime.sendMessage({ type: "ttsStop" })`

### 9.2 Background runs TTS logic

- **File**: `core/ttsService.js`

Two modes:

- **Proxy mode** (`settings.ttsProvider === "proxy"`):
  - calls `http://127.0.0.1:8788/tts`
  - expects base64 audio back
  - the UI plays it using `<audio>` with a `data:` URL
- **Browser mode**:
  - uses `chrome.tts.speak(...)`

## 10) A simple “repeatable mental model”

- **Settings** live in `chrome.storage.local` and are managed by `OM.settings`.
- **Translation requests** are:
  - initiated by content script / result window via messages (`type: "translate"`), or
  - initiated by context menu (background directly).
- **Actual HTTP calls** go to your local proxy on `127.0.0.1:8788`.
- **Result window UI** updates by listening to storage changes on `translation:<id>`.

