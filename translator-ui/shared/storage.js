(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const ensureChromeStorage = () => {
    if (!globalThis.chrome?.storage?.local) {
      throw new Error('chrome.storage.local not available');
    }
  };

  OM.storage = Object.freeze({
    async get(keys) {
      ensureChromeStorage();
      if (OM.chromeStorageRepo?.getLocal) {
        return await OM.chromeStorageRepo.getLocal(keys);
      }
      return await new Promise((resolve) => {
        chrome.storage.local.get(keys, (res) => resolve(res || {}));
      });
    },
    async set(obj) {
      ensureChromeStorage();
      if (OM.chromeStorageRepo?.setLocal) {
        await OM.chromeStorageRepo.setLocal(obj);
        return;
      }
      await new Promise((resolve) => {
        chrome.storage.local.set(obj, () => resolve());
      });
    },
    async remove(keys) {
      ensureChromeStorage();
      if (OM.chromeStorageRepo?.removeLocal) {
        await OM.chromeStorageRepo.removeLocal(keys);
        return;
      }
      await new Promise((resolve) => {
        chrome.storage.local.remove(keys, () => resolve());
      });
    },
  });
})();

