(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const ensure = () => {
    if (!globalThis.chrome?.storage?.local) {
      throw new Error('chrome.storage.local not available');
    }
  };

  OM.chromeStorageRepo = Object.freeze({
    getLocal(keys) {
      ensure();
      return new Promise((resolve, reject) => {
        try {
          chrome.storage.local.get(keys, (res) => {
            if (chrome.runtime?.lastError) {
              reject(new Error(chrome.runtime.lastError.message));
              return;
            }
            resolve(res || {});
          });
        } catch (err) {
          reject(err);
        }
      });
    },

    setLocal(obj) {
      ensure();
      return new Promise((resolve, reject) => {
        try {
          chrome.storage.local.set(obj, () => {
            if (chrome.runtime?.lastError) {
              reject(new Error(chrome.runtime.lastError.message));
              return;
            }
            resolve();
          });
        } catch (err) {
          reject(err);
        }
      });
    },

    removeLocal(keys) {
      ensure();
      return new Promise((resolve, reject) => {
        try {
          chrome.storage.local.remove(keys, () => {
            if (chrome.runtime?.lastError) {
              reject(new Error(chrome.runtime.lastError.message));
              return;
            }
            resolve();
          });
        } catch (err) {
          reject(err);
        }
      });
    },
  });
})();

