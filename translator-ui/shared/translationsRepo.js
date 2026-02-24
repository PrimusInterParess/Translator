(() => {
  const OM = (globalThis.OM = globalThis.OM || {});
  const C = OM.constants;

  const HISTORY_MAX = 20;

  const keyForId = (id) => `${C.TRANSLATION_KEY_PREFIX}${id}`;

  const createId = () => {
    try {
      if (globalThis.crypto?.randomUUID) return globalThis.crypto.randomUUID();
    } catch {
      // ignore
    }
    return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  };

  const loadHistory = async () => {
    const res = await OM.storage.get([C.STORAGE_KEYS.translationHistory]);
    const arr = res[C.STORAGE_KEYS.translationHistory];
    return Array.isArray(arr) ? arr.filter((x) => typeof x === 'string' && x) : [];
  };

  const saveHistory = async (ids) => {
    await OM.storage.set({ [C.STORAGE_KEYS.translationHistory]: ids });
  };

  const pushToHistory = async (id) => {
    const current = await loadHistory();
    if (current[0] === id && current.length <= HISTORY_MAX) return current;
    const next = [id, ...current.filter((x) => x !== id)].slice(0, HISTORY_MAX);
    await saveHistory(next);

    // Best-effort cleanup for trimmed IDs.
    const trimmed = current.filter((x) => x !== id).slice(HISTORY_MAX - 1);
    if (trimmed.length) {
      try {
        await OM.storage.remove(trimmed.map((x) => keyForId(x)));
      } catch {
        // ignore cleanup failures
      }
    }

    return next;
  };

  OM.translationsRepo = Object.freeze({
    HISTORY_MAX,
    keyForId,
    createId,

    async getLastId() {
      const res = await OM.storage.get([C.STORAGE_KEYS.lastTranslationId]);
      const id = res[C.STORAGE_KEYS.lastTranslationId];
      return typeof id === 'string' && id ? id : '';
    },

    async setLastId(id) {
      await OM.storage.set({ [C.STORAGE_KEYS.lastTranslationId]: id });
    },

    async get(id) {
      const res = await OM.storage.get([keyForId(id)]);
      return res[keyForId(id)] || null;
    },

    async set(id, record) {
      await OM.storage.set({
        [keyForId(id)]: record,
        [C.STORAGE_KEYS.lastTranslationId]: id,
      });
      await pushToHistory(id);
      return id;
    },

    async update(id, patch) {
      const current = (await OM.translationsRepo.get(id)) || {};
      const next = { ...current, ...(patch || {}) };
      await OM.translationsRepo.set(id, next);
      return next;
    },

    async listHistory() {
      return await loadHistory();
    },
  });
})();

