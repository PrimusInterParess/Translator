(() => {
  // kari e noremrgkvc gwejfdc
  const OM = (globalThis.OM = globalThis.OM || {});

  async function updateActionBadge(enabled) {
    try {
      if (!globalThis.chrome?.action?.setBadgeText) return;

      const isOn = enabled !== false;
      await chrome.action.setBadgeText({ text: isOn ? 'ON' : 'OFF' });
      if (chrome.action.setBadgeBackgroundColor) {
        await chrome.action.setBadgeBackgroundColor({ color: isOn ? '#1a73e8' : '#d93025' });
      }
      if (chrome.action.setTitle) {
        await chrome.action.setTitle({ title: isOn ? 'OversætMig (enabled)' : 'OversætMig (paused)' });
      }
    } catch {
      // ignore badge errors (e.g. unsupported in some environments)
    }
  }

  async function syncBadgeFromSettings() {
    try {
      const settings = await OM.settings.get();
      await updateActionBadge(settings.enabled);
    } catch {
      await updateActionBadge(true);
    }
  }

  async function syncContextMenuFromSettings() {
    try {
      const settings = await OM.settings.get();
      await OM.backgroundHandlers.syncContextMenu(settings.enabled);
    } catch {
      await OM.backgroundHandlers.syncContextMenu(true);
    }
  }

  OM.background = Object.freeze({
    init() {
      syncBadgeFromSettings().catch(() => {});
      syncContextMenuFromSettings().catch(() => {});

      if (globalThis.chrome?.storage?.onChanged?.addListener) {
        chrome.storage.onChanged.addListener((changes, areaName) => {
          if (areaName !== 'local') return;
          const ch = changes?.[OM.constants?.STORAGE_KEYS?.enabled];
          if (!ch) return;
          updateActionBadge(ch.newValue).catch(() => {});
          OM.backgroundHandlers.syncContextMenu(ch.newValue).catch(() => {});
        });
      }

      chrome.runtime.onInstalled.addListener(() => {
        OM.backgroundHandlers.onInstalled().catch(() => { });
      });

      chrome.contextMenus.onClicked.addListener((info) => {
        OM.backgroundHandlers.onContextMenuClick(info).catch(() => { });
      });

      chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
        (async () => {
          try {
            const res = await OM.backgroundHandlers.onMessage(msg, sender);
            sendResponse(res);
          } catch (err) {
            sendResponse({ ok: false, error: err?.message || String(err) });
          }
        })();
        return true;
      });
    },
  });
})();

