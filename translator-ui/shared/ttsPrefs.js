(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const guessSpeakLang = (text) => {
    const t = typeof text === 'string' ? text : '';
    if (/[\u0400-\u04FF]/.test(t)) return 'bg'; // Cyrillic (best-effort default)
    if (/[\u0370-\u03FF]/.test(t)) return 'el'; // Greek
    if (/[\u0600-\u06FF]/.test(t)) return 'ar'; // Arabic
    if (/[\u4E00-\u9FFF]/.test(t)) return 'zh-CN'; // CJK (best-effort)
    if (/[æøå]/i.test(t)) return 'da'; // Danish-specific letters
    return 'en';
  };

  const getSpeechPrefs = async () => {
    try {
      const settings = await OM.runtime.sendMessage({ type: 'getSettings' });
      if (settings?.ok) {
        const rate = typeof settings.ttsRate === 'number' ? settings.ttsRate : Number(settings.ttsRate);
        const volume = typeof settings.ttsVolume === 'number' ? settings.ttsVolume : Number(settings.ttsVolume);
        return {
          rate: Number.isFinite(rate) ? rate : 1.0,
          volume: Number.isFinite(volume) ? volume : 0.8,
        };
      }
    } catch {
      // ignore
    }
    return { rate: 1.0, volume: 0.8 };
  };

  OM.ttsPrefs = Object.freeze({
    guessSpeakLang,
    getSpeechPrefs,
  });
})();

