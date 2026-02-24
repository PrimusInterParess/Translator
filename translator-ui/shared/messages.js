(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const TYPES = Object.freeze({
    getSettings: 'getSettings',
    translate: 'translate',
    closeResultWindow: 'closeResultWindow',
    ttsSpeak: 'ttsSpeak',
    ttsInfo: 'ttsInfo',
    ttsStop: 'ttsStop',
  });

  const ERROR_CODES = Object.freeze({
    missingText: 'MISSING_TEXT',
    textTooLong: 'TEXT_TOO_LONG',
    paused: 'PAUSED',
    network: 'NETWORK',
    service: 'SERVICE',
    internal: 'INTERNAL',
    invalidMessage: 'INVALID_MESSAGE',
    unknownMessageType: 'UNKNOWN_MESSAGE_TYPE',
  });

  OM.messages = Object.freeze({
    TYPES,
    ERROR_CODES,
  });
})();

