(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const DEFAULT_STRINGS = Object.freeze({
    generic: 'Sorry — I couldn’t translate that right now. Please try again.',
    missingText: 'Please enter text to translate.',
    network: 'I couldn’t reach the translation service. Check your internet connection and try again.',
    service: 'The translation service responded in an unexpected way. Please try again in a moment.',
    internal: 'Something went wrong in the extension. Please reload and try again.',
    paused: 'Extension is paused',
  });

  const getErrStrings = (outputEl) => {
    const ds = outputEl?.dataset || {};
    return {
      generic: ds.errGeneric || DEFAULT_STRINGS.generic,
      network: ds.errNetwork || DEFAULT_STRINGS.network,
      service: ds.errService || DEFAULT_STRINGS.service,
      internal: ds.errInternal || DEFAULT_STRINGS.internal,
    };
  };

  const normalizeErrorMessage = (err) => {
    if (!err) return '';
    if (typeof err === 'string') return err.trim();
    if (typeof err?.error === 'string') return err.error.trim();
    if (typeof err?.message === 'string') return err.message.trim();
    return String(err).trim();
  };

  const normalizeErrorCode = (err) => {
    const c = typeof err?.errorCode === 'string' ? err.errorCode.trim() : '';
    return c ? c.toUpperCase() : '';
  };

  // Back-compat classifier: turns old string errors into stable-ish buckets.
  const classifyFromMessage = (msg) => {
    const lower = (msg || '').toLowerCase();
    if (!lower) return 'UNKNOWN';
    if (lower.includes('text is too long')) return 'TEXT_TOO_LONG';
    if (lower.includes('missing text')) return 'MISSING_TEXT';
    if (lower.includes('paused')) return 'PAUSED';
    if (lower.includes('failed to fetch') || lower.includes('networkerror')) return 'NETWORK';
    if (lower.includes('unexpected api response')) return 'SERVICE';
    if (lower.includes('invalid message') || lower.includes('unknown message type')) return 'INTERNAL';
    return 'UNKNOWN';
  };

  const toUserMessage = ({ errorCode, message }, outputEl) => {
    const { generic, network, service, internal } = getErrStrings(outputEl);

    const code = (errorCode || '').toUpperCase();
    if (code === 'MISSING_TEXT') return DEFAULT_STRINGS.missingText;
    if (code === 'TEXT_TOO_LONG') return message || generic;
    if (code === 'PAUSED') return message || DEFAULT_STRINGS.paused;
    if (code === 'NETWORK') return network;
    if (code === 'SERVICE') return service;
    if (code === 'INTERNAL') return internal;

    // Fall back to message heuristics (old behavior).
    const guessed = classifyFromMessage(message);
    if (guessed === 'MISSING_TEXT') return DEFAULT_STRINGS.missingText;
    if (guessed === 'TEXT_TOO_LONG') return message;
    if (guessed === 'PAUSED') return message || DEFAULT_STRINGS.paused;
    if (guessed === 'NETWORK') return network;
    if (guessed === 'SERVICE') return service;
    if (guessed === 'INTERNAL') return internal;
    return generic;
  };

  OM.errors = Object.freeze({
    DEFAULT_STRINGS,
    getErrStrings,
    normalizeErrorMessage,
    normalizeErrorCode,
    classifyFromMessage,
    toUserMessage,
    // Convenience wrapper for existing call-sites.
    friendlyTranslateError(err, outputEl) {
      const message = normalizeErrorMessage(err);
      const errorCode = normalizeErrorCode(err) || classifyFromMessage(message);
      return toUserMessage({ errorCode, message }, outputEl);
    },
  });
})();

