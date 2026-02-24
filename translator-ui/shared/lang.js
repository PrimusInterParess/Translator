(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const normalizeLang = (lang) =>
    typeof lang === 'string' ? lang.trim().toLowerCase().replace('_', '-') : '';

  const docLang = () => {
    try {
      if (typeof document === 'undefined') return '';
      return normalizeLang(document.documentElement?.lang);
    } catch {
      return '';
    }
  };

  const guessFromUnicode = (t) => {
    if (/[\u0400-\u04FF]/.test(t)) return 'bg'; // Cyrillic (best-effort default)
    if (/[\u0370-\u03FF]/.test(t)) return 'el'; // Greek
    if (/[\u0600-\u06FF]/.test(t)) return 'ar'; // Arabic
    if (/[\u4E00-\u9FFF]/.test(t)) return 'zh-CN'; // CJK (best-effort)
    return 'en';
  };

  OM.lang = Object.freeze({
    normalizeLang,
    guessSourceLang(text, opts) {
      const t = typeof text === 'string' ? text : '';
      const preferred = normalizeLang(opts?.documentLang) || docLang();
      if (preferred) return preferred;
      return guessFromUnicode(t);
    },
  });
})();

