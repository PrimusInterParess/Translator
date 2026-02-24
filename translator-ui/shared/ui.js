(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  OM.ui = Object.freeze({
    setLoading(el, isLoading) {
      if (!el || !el.classList) return;
      el.classList.toggle('is-loading', !!isLoading);
      try {
        if (isLoading) el.setAttribute('aria-busy', 'true');
        else el.removeAttribute('aria-busy');
      } catch {
        // ignore
      }
    },
  });
})();

