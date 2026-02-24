(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const errRes = (errorCode, error) => ({ ok: false, errorCode, error });

  const normalizeErrMsg = (err) => {
    if (!err) return '';
    if (typeof err === 'string') return err.trim();
    if (typeof err?.message === 'string') return err.message.trim();
    return String(err).trim();
  };

  const classifyErrorCode = (err) => {
    const msg = normalizeErrMsg(err);
    const lower = msg.toLowerCase();
    const E = OM.messages?.ERROR_CODES;
    if (!msg) return E?.internal || 'INTERNAL';
    if (lower.includes('text is too long')) return E?.textTooLong || 'TEXT_TOO_LONG';
    if (lower.includes('missing text')) return E?.missingText || 'MISSING_TEXT';
    if (lower.includes('paused')) return E?.paused || 'PAUSED';
    if (lower.includes('failed to fetch') || lower.includes('networkerror') || lower.includes('http ')) {
      return E?.network || 'NETWORK';
    }
    if (lower.includes('unexpected api response')) return E?.service || 'SERVICE';
    return E?.internal || 'INTERNAL';
  };

  OM.translatorService = Object.freeze({
    async translateFromMessage({ rawText, guessedSource } = {}) {
      const E = OM.messages?.ERROR_CODES;

      const settings = await OM.settings.get();
      if (settings.enabled === false) {
        return errRes(E?.paused || 'PAUSED', 'Extension is paused');
      }

      const text = OM.text.sanitize(rawText);
      if (!text) return errRes(E?.missingText || 'MISSING_TEXT', 'Missing text');
      if (OM.text.isTooLong(rawText)) {
        return errRes(E?.textTooLong || 'TEXT_TOO_LONG', `Text is too long (max ${OM.constants.MAX_TEXT_LEN} chars).`);
      }

      let source = settings.sourceLang;
      const target = settings.targetLang;

      if (source === 'auto') {
        source =
          typeof guessedSource === 'string' && guessedSource
            ? guessedSource
            : OM.lang.guessSourceLang(text);
      }

      try {
        const translatedText = await OM.mymemory.translate({
          text,
          source,
          target,
          email: settings.email,
        });
        return { ok: true, translatedText, source, target, text };
      } catch (err) {
        const code = classifyErrorCode(err);
        return errRes(code, normalizeErrMsg(err) || 'Translate failed');
      }
    },
  });
})();

