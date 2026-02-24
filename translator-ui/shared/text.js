(() => {
  const OM = (globalThis.OM = globalThis.OM || {});
  const C = OM.constants;

  OM.text = Object.freeze({
    sanitize(input) {
      const raw = typeof input === 'string' ? input : '';
      const trimmed = raw.trim();
      return trimmed.slice(0, C.MAX_TEXT_LEN);
    },
    isTooLong(input) {
      const raw = typeof input === 'string' ? input : '';
      return raw.trim().length > C.MAX_TEXT_LEN;
    },
  });
})();

