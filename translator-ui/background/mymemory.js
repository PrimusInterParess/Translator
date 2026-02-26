(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  OM.mymemory = Object.freeze({
    async translate({ text, source, target, email }) {
      // Use 127.0.0.1 (IPv4) to avoid localhost/IPv6 ambiguity on Windows.
      const url = 'http://127.0.0.1:8788/translate/mymemory';

      const r = await fetch(url, {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ text, source, target, email }),
      });

      const d = await (async () => {
        try {
          return await r.json();
        } catch {
          return null;
        }
      })();

      if (!r.ok) {
        const statusText = r.status ? `HTTP ${r.status}` : 'HTTP error';
        const msg = typeof d?.error === 'string' && d.error.trim() ? d.error.trim() : statusText;
        throw new Error(msg);
      }

      const translatedText = d?.translatedText;
      if (d?.ok === true && typeof translatedText === 'string' && translatedText.trim()) return translatedText;

      const msg = typeof d?.error === 'string' && d.error.trim() ? d.error.trim() : 'Unexpected API response';
      throw new Error(msg);
    },
  });
})();

