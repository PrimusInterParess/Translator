(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const NS = 'http://www.w3.org/2000/svg';

  const createSpeakerSvg = () => {
    const svg = document.createElementNS(NS, 'svg');
    svg.setAttribute('viewBox', '0 0 24 24');
    svg.setAttribute('aria-hidden', 'true');
    svg.setAttribute('focusable', 'false');

    const path = document.createElementNS(NS, 'path');
    path.setAttribute(
      'd',
      'M4 10v4c0 1.1.9 2 2 2h3l4 4c.6.6 1.5.2 1.5-.7V4.7c0-.9-.9-1.3-1.5-.7l-4 4H6c-1.1 0-2 .9-2 2zm14.5 2c0-1.8-1-3.4-2.5-4.2v8.4c1.5-.8 2.5-2.4 2.5-4.2zm-2.5-9.2v2.1c2.9 1 5 3.8 5 7.1s-2.1 6.1-5 7.1v2.1c4-1.1 7-4.8 7-9.2s-3-8.1-7-9.2z'
    );
    svg.appendChild(path);
    return svg;
  };

  OM.icons = Object.freeze({
    createSpeakerSvg,
    applySpeakerIconToButton(btn) {
      if (!btn) return;
      try {
        btn.textContent = '';
        btn.appendChild(createSpeakerSvg());
      } catch {
        // Fallback: at least show something clickable.
        btn.textContent = 'Listen';
      }
    },
  });
})();

