(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const safeJson = async (r) => {
    try {
      return await r.json();
    } catch {
      return null;
    }
  };

  OM.http = Object.freeze({
    async getJson(url, { headers } = {}) {
      const r = await fetch(url, { headers });
      const data = await safeJson(r);
      if (!r.ok) {
        const statusText = r.status ? `HTTP ${r.status}` : 'HTTP error';
        const msg = typeof data?.message === 'string' ? data.message : statusText;
        const err = new Error(msg);
        err.status = r.status;
        throw err;
      }
      return data;
    },
  });
})();

