(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  OM.chromeRuntimeTransport = Object.freeze({
    sendMessage(msg) {
      return new Promise((resolve, reject) => {
        try {
          if (!globalThis.chrome?.runtime?.sendMessage) {
            reject(new Error('chrome.runtime.sendMessage not available'));
            return;
          }
          chrome.runtime.sendMessage(msg, (res) => {
            if (chrome.runtime.lastError) {
              reject(new Error(chrome.runtime.lastError.message));
              return;
            }
            resolve(res);
          });
        } catch (err) {
          reject(err);
        }
      });
    },
  });
})();

