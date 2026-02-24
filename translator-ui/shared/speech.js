(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const isSupported = () =>
    typeof globalThis !== 'undefined' &&
    !!globalThis.speechSynthesis &&
    typeof globalThis.SpeechSynthesisUtterance === 'function';

  const normalizeLang = (lang) => {
    const raw = typeof lang === 'string' ? lang.trim() : '';
    if (!raw) return '';
    // Keep it simple: pass through BCP-47-ish tags, normalize underscores.
    return raw.replace('_', '-');
  };

  const getVoicesSafe = () => {
    try {
      if (!isSupported()) return [];
      const v = globalThis.speechSynthesis.getVoices();
      return Array.isArray(v) ? v : [];
    } catch {
      return [];
    }
  };

  const pickVoice = (voices, lang) => {
    const l = normalizeLang(lang).toLowerCase();
    if (!l || !Array.isArray(voices) || voices.length === 0) return null;

    const exact = voices.find((v) => typeof v?.lang === 'string' && v.lang.toLowerCase() === l);
    if (exact) return exact;

    const base = l.split('-')[0];
    if (!base) return null;
    return (
      voices.find((v) => typeof v?.lang === 'string' && v.lang.toLowerCase().startsWith(`${base}-`)) ||
      null
    );
  };

  const waitForVoices = (timeoutMs = 1200) =>
    new Promise((resolve) => {
      const initial = getVoicesSafe();
      if (initial.length) {
        resolve(initial);
        return;
      }

      if (!isSupported()) {
        resolve([]);
        return;
      }

      let done = false;
      const finish = () => {
        if (done) return;
        done = true;
        try {
          globalThis.speechSynthesis.removeEventListener('voiceschanged', onVoicesChanged);
        } catch {
          // ignore
        }
        resolve(getVoicesSafe());
      };

      const onVoicesChanged = () => finish();

      try {
        globalThis.speechSynthesis.addEventListener('voiceschanged', onVoicesChanged);
      } catch {
        // ignore
      }

      setTimeout(() => finish(), timeoutMs);
    });

  OM.speech = Object.freeze({
    isSupported,

    stop() {
      try {
        if (!isSupported()) return { ok: false, error: 'Speech synthesis not supported' };
        globalThis.speechSynthesis.cancel();
        return { ok: true };
      } catch (err) {
        return { ok: false, error: err?.message || String(err) };
      }
    },

    async speak({ text, lang, rate, volume, pitch } = {}) {
      const t = typeof text === 'string' ? text.trim() : '';
      if (!t) return { ok: false, error: 'Missing text' };

      try {
        if (!isSupported()) return { ok: false, error: 'Speech synthesis not supported' };

        const u = new globalThis.SpeechSynthesisUtterance(t);
        // Ensure voices are loaded (some browsers return [] initially).
        const voices = (await waitForVoices()) || [];
        const voice = pickVoice(voices, lang);
        if (voice) {
          u.voice = voice;
          if (typeof voice.lang === 'string' && voice.lang) u.lang = voice.lang;
        }

        const r = typeof rate === 'number' ? rate : Number(rate);
        const v = typeof volume === 'number' ? volume : Number(volume);
        const p = typeof pitch === 'number' ? pitch : Number(pitch);

        u.volume = Number.isFinite(v) ? Math.min(1, Math.max(0, v)) : 0.8;
        u.rate = Number.isFinite(r) ? Math.min(3, Math.max(0.1, r)) : 1.0;
        u.pitch = Number.isFinite(p) ? Math.min(2, Math.max(0, p)) : 1;

        // Attempt to unpause if the synth is paused.
        try {
          if (globalThis.speechSynthesis.paused) globalThis.speechSynthesis.resume();
        } catch {
          // ignore
        }

        // Resolve when we know it started (or errored), so callers can show feedback.
        return await new Promise((resolve) => {
          let settled = false;
          const settle = (res) => {
            if (settled) return;
            settled = true;
            u.onstart = null;
            u.onerror = null;
            resolve(res);
          };

          u.onstart = () => settle({ ok: true });
          u.onerror = (e) => {
            const msg = e?.error ? `Speech error: ${e.error}` : 'Speech error';
            settle({ ok: false, error: msg });
          };

          // If it never starts, treat as failure (often no voices / audio blocked).
          setTimeout(() => {
            settle({ ok: false, error: 'Speech did not start (blocked or no voices available).' });
          }, 2500);

          // Prevent overlapping speech. Using a short delay avoids a cancel/speak race in some engines.
          try {
            globalThis.speechSynthesis.cancel();
          } catch {
            // ignore
          }
          setTimeout(() => {
            try {
              globalThis.speechSynthesis.speak(u);
            } catch (err) {
              settle({ ok: false, error: err?.message || String(err) });
            }
          }, 0);
        });
      } catch (err) {
        return { ok: false, error: err?.message || String(err) };
      }
    },
  });
})();

