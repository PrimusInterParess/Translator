(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  OM.resultWindow = Object.freeze({
    async open(requestId) {
      const popupWidth = 420;
      const popupHeight = 560;
      const margin = 16;

      const currentWin = await new Promise((resolve) => {
        try {
          chrome.windows.getLastFocused({}, (w) => resolve(w || null));
        } catch {
          resolve(null);
        }
      });

      const left =
        currentWin?.left != null && currentWin?.width != null
          ? Math.max(0, currentWin.left + currentWin.width - popupWidth - margin)
          : undefined;

      const top =
        currentWin?.top != null ? Math.max(0, currentWin.top + margin) : undefined;

      const baseUrl = chrome.runtime.getURL('result.html');
      const url =
        typeof requestId === 'string' && requestId ? `${baseUrl}?id=${encodeURIComponent(requestId)}` : baseUrl;

      await chrome.windows.create({
        url,
        type: 'popup',
        width: popupWidth,
        height: popupHeight,
        left,
        top,
      });
    },
  });
})();

