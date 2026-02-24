(() => {
  // Global namespace for the extension (works in pages, content scripts, service worker).
  const root = globalThis;
  root.OM = root.OM || {};
})();

