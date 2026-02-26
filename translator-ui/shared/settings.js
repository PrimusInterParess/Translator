(() => {
  const OM = (globalThis.OM = globalThis.OM || {});
  const C = OM.constants;

  const normalizeEmail = (email) => (typeof email === 'string' ? email.trim() : '');
  const normalizeEnabled = (v) => v !== false;
  const normalizeText = (v) => (typeof v === 'string' ? v.trim() : '');
  const normalizeTtsProvider = (v) => {
    const s = normalizeText(v).toLowerCase();
    if (s === 'proxy') return 'proxy';
    return 'browser';
  };
  const normalizeProxyUrl = (v) => {
    const s = normalizeText(v);
    if (!s) return '';
    try {
      const u = new URL(s);
      if (u.protocol !== 'http:' && u.protocol !== 'https:') return '';
      return u.toString();
    } catch {
      return '';
    }
  };
  const normalizeNumber = (v, fallback) => {
    const n = typeof v === 'number' ? v : Number(v);
    return Number.isFinite(n) ? n : fallback;
  };
  const clamp = (n, min, max) => Math.min(max, Math.max(min, n));
  const normalizeTtsRate = (v) => clamp(normalizeNumber(v, C.DEFAULTS.ttsRate), 0.7, 1.4);
  const normalizeTtsVolume = (v) => clamp(normalizeNumber(v, C.DEFAULTS.ttsVolume), 0.2, 1);
  const normalizeGooglePitch = (v) => clamp(normalizeNumber(v, C.DEFAULTS.googlePitch), -20, 20);
  const normalizeHoldToTranslateMs = (v) =>
    clamp(Math.round(normalizeNumber(v, C.DEFAULTS.holdToTranslateMs)), 0, 5000);
  const normalizeResultAutoCloseMs = (v) =>
    clamp(Math.round(normalizeNumber(v, C.DEFAULTS.resultAutoCloseMs)), 0, 300000);

  OM.settings = Object.freeze({
    async get() {
      const keys = [
        C.STORAGE_KEYS.enabled,
        C.STORAGE_KEYS.email,
        C.STORAGE_KEYS.sourceLang,
        C.STORAGE_KEYS.targetLang,
        C.STORAGE_KEYS.holdToTranslateMs,
        C.STORAGE_KEYS.resultAutoCloseMs,
        C.STORAGE_KEYS.ttsRate,
        C.STORAGE_KEYS.ttsVolume,
        C.STORAGE_KEYS.ttsProvider,
        C.STORAGE_KEYS.googleVoiceName,
        C.STORAGE_KEYS.googleLanguageCode,
        C.STORAGE_KEYS.googlePitch,
        C.STORAGE_KEYS.ttsProxyUrl,
      ];
      const res = await OM.storage.get(keys);
      return {
        enabled: normalizeEnabled(res[C.STORAGE_KEYS.enabled] ?? C.DEFAULTS.enabled),
        email: normalizeEmail(res[C.STORAGE_KEYS.email]),
        sourceLang: res[C.STORAGE_KEYS.sourceLang] || C.DEFAULTS.sourceLang,
        targetLang: res[C.STORAGE_KEYS.targetLang] || C.DEFAULTS.targetLang,
        holdToTranslateMs: normalizeHoldToTranslateMs(res[C.STORAGE_KEYS.holdToTranslateMs]),
        resultAutoCloseMs: normalizeResultAutoCloseMs(res[C.STORAGE_KEYS.resultAutoCloseMs]),
        ttsRate: normalizeTtsRate(res[C.STORAGE_KEYS.ttsRate]),
        ttsVolume: normalizeTtsVolume(res[C.STORAGE_KEYS.ttsVolume]),
        ttsProvider: normalizeTtsProvider(res[C.STORAGE_KEYS.ttsProvider] ?? C.DEFAULTS.ttsProvider),
        googleVoiceName: normalizeText(res[C.STORAGE_KEYS.googleVoiceName] ?? C.DEFAULTS.googleVoiceName),
        googleLanguageCode: normalizeText(res[C.STORAGE_KEYS.googleLanguageCode] ?? C.DEFAULTS.googleLanguageCode),
        googlePitch: normalizeGooglePitch(res[C.STORAGE_KEYS.googlePitch]),
        ttsProxyUrl: normalizeProxyUrl(res[C.STORAGE_KEYS.ttsProxyUrl] ?? C.DEFAULTS.ttsProxyUrl),
      };
    },
    async setEnabled(enabled) {
      await OM.storage.set({ [C.STORAGE_KEYS.enabled]: normalizeEnabled(enabled) });
    },
    async setEmail(email) {
      await OM.storage.set({ [C.STORAGE_KEYS.email]: normalizeEmail(email) });
    },
    async setSourceLang(sourceLang) {
      await OM.storage.set({ [C.STORAGE_KEYS.sourceLang]: sourceLang });
    },
    async setTargetLang(targetLang) {
      await OM.storage.set({ [C.STORAGE_KEYS.targetLang]: targetLang });
    },
    async setHoldToTranslateMs(holdToTranslateMs) {
      await OM.storage.set({ [C.STORAGE_KEYS.holdToTranslateMs]: normalizeHoldToTranslateMs(holdToTranslateMs) });
    },
    async setResultAutoCloseMs(resultAutoCloseMs) {
      await OM.storage.set({ [C.STORAGE_KEYS.resultAutoCloseMs]: normalizeResultAutoCloseMs(resultAutoCloseMs) });
    },
    async setTtsRate(ttsRate) {
      await OM.storage.set({ [C.STORAGE_KEYS.ttsRate]: normalizeTtsRate(ttsRate) });
    },
    async setTtsVolume(ttsVolume) {
      await OM.storage.set({ [C.STORAGE_KEYS.ttsVolume]: normalizeTtsVolume(ttsVolume) });
    },
    async setTtsProvider(ttsProvider) {
      await OM.storage.set({ [C.STORAGE_KEYS.ttsProvider]: normalizeTtsProvider(ttsProvider) });
    },
    async setGoogleVoiceName(googleVoiceName) {
      await OM.storage.set({ [C.STORAGE_KEYS.googleVoiceName]: normalizeText(googleVoiceName) });
    },
    async setGoogleLanguageCode(googleLanguageCode) {
      await OM.storage.set({ [C.STORAGE_KEYS.googleLanguageCode]: normalizeText(googleLanguageCode) });
    },
    async setGooglePitch(googlePitch) {
      await OM.storage.set({ [C.STORAGE_KEYS.googlePitch]: normalizeGooglePitch(googlePitch) });
    },
    async setTtsProxyUrl(ttsProxyUrl) {
      await OM.storage.set({ [C.STORAGE_KEYS.ttsProxyUrl]: normalizeProxyUrl(ttsProxyUrl) });
    },
  });
})();

