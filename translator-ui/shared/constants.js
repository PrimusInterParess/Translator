(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  OM.constants = Object.freeze({
    MAX_TEXT_LEN: 500,
    TRANSLATION_KEY_PREFIX: 'translation:',
    STORAGE_KEYS: Object.freeze({
      enabled: 'enabled',
      email: 'email',
      sourceLang: 'sourceLang',
      targetLang: 'targetLang',
      holdToTranslateMs: 'holdToTranslateMs',
      resultAutoCloseMs: 'resultAutoCloseMs',
      lastTranslation: 'lastTranslation', // legacy; keep for back-compat
      lastTranslationId: 'lastTranslationId',
      translationHistory: 'translationHistory',
      ttsRate: 'ttsRate',
      ttsVolume: 'ttsVolume',
      ttsProvider: 'ttsProvider',
      googleVoiceName: 'googleVoiceName',
      googleLanguageCode: 'googleLanguageCode',
      googlePitch: 'googlePitch',
      ttsProxyUrl: 'ttsProxyUrl',
    }),
    DEFAULTS: Object.freeze({
      enabled: true,
      sourceLang: 'da',
      targetLang: 'bg',
      holdToTranslateMs: 2000,
      resultAutoCloseMs: 25000,
      ttsRate: 1.0,
      ttsVolume: 0.8,
      ttsProvider: 'browser',
      googleVoiceName: 'da-DK-Standard-A',
      googleLanguageCode: 'da-DK',
      googlePitch: 0,
      // Use 127.0.0.1 (IPv4) to avoid localhost/IPv6 ambiguity on Windows.
      ttsProxyUrl: 'http://127.0.0.1:8788/tts',
    }),
    CONTEXT_MENU_ID: 'qt-translate-selection',
  });
})();

