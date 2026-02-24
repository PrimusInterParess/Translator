(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  OM.runtime = Object.freeze({
    sendMessage(msg) {
      return new Promise((resolve) => {
        try {
          if (OM.chromeRuntimeTransport?.sendMessage) {
            OM.chromeRuntimeTransport
              .sendMessage(msg)
              .then((res) => resolve(res || { ok: false, error: 'No response' }))
              .catch((err) => resolve({ ok: false, error: err?.message || String(err) }));
            return;
          }

          if (!globalThis.chrome?.runtime?.sendMessage) {
            resolve({ ok: false, error: 'chrome.runtime.sendMessage not available' });
            return;
          }
          chrome.runtime.sendMessage(msg, (res) => {
            if (chrome.runtime.lastError) {
              resolve({ ok: false, error: chrome.runtime.lastError.message });
              return;
            }
            resolve(res || { ok: false, error: 'No response' });
          });
        } catch (err) {
          resolve({ ok: false, error: err?.message || String(err) });
        }
      });
    },
  });
})();

