(() => {
  const OM = (globalThis.OM = globalThis.OM || {});
  const C = OM.constants;

  const errRes = (errorCode, error) => ({ ok: false, errorCode, error });

  const normalizeLang = (lang) =>
    typeof lang === 'string' && lang.trim() ? lang.trim().toLowerCase().replace('_', '-') : '';

  const normalizeGoogleLang = (lang) => (typeof lang === 'string' && lang.trim() ? lang.trim().replace('_', '-') : '');

  const clamp = (n, min, max) => Math.min(max, Math.max(min, n));

  const canUseProxy = (settings) => {
    if (settings?.ttsProvider !== 'proxy') return false;
    try {
      const u = new URL(C?.DEFAULTS?.ttsProxyUrl || '');
      return u.protocol === 'http:' || u.protocol === 'https:';
    } catch {
      return false;
    }
  };

  OM.ttsService = Object.freeze({
    async speakFromMessage({ text, lang } = {}) {
      const E = OM.messages?.ERROR_CODES;

      const settings = await OM.settings.get();
      if (settings.enabled === false) return errRes(E?.paused || 'PAUSED', 'Extension is paused');

      const t = OM.text.sanitize(text);
      if (!t) return errRes(E?.missingText || 'MISSING_TEXT', 'Missing text');
      if (OM.text.isTooLong(text)) {
        return errRes(E?.textTooLong || 'TEXT_TOO_LONG', 'Text is too long (max 500 chars).');
      }

      if (canUseProxy(settings)) {
        const languageCode =
          typeof settings.googleLanguageCode === 'string' && settings.googleLanguageCode.trim()
            ? settings.googleLanguageCode.trim()
            : normalizeGoogleLang(lang) || C?.DEFAULTS?.googleLanguageCode || 'en-US';
        const proxyUrl = C?.DEFAULTS?.ttsProxyUrl || '';

        try {
          const r = await fetch(proxyUrl, {
            method: 'POST',
            headers: { 'content-type': 'application/json' },
            body: JSON.stringify({
              text: t,
              languageCode,
              voiceName: settings.googleVoiceName,
              speakingRate: settings.ttsRate,
              pitch: settings.googlePitch,
            }),
          });
          const data = await (async () => {
            try {
              return await r.json();
            } catch {
              return null;
            }
          })();

          if (!r.ok) {
            // Proxy failed; fall back to OS/Chrome TTS below.
          }

          if (data?.ok === true && data?.audio?.base64) return data;
          // Proxy returned invalid response; fall back to OS/Chrome TTS below.
        } catch (err) {
          // Proxy network error; fall back to OS/Chrome TTS below.
        }
      }

      if (!globalThis.chrome?.tts?.speak) {
        return errRes(E?.internal || 'INTERNAL', 'chrome.tts not available');
      }

      const normalizedLang = normalizeLang(lang);

      const rate = (() => {
        const n = typeof settings.ttsRate === 'number' ? settings.ttsRate : Number(settings.ttsRate);
        if (!Number.isFinite(n)) return 1.0;
        return clamp(n, 0.7, 1.4);
      })();

      const volume = (() => {
        const n = typeof settings.ttsVolume === 'number' ? settings.ttsVolume : Number(settings.ttsVolume);
        if (!Number.isFinite(n)) return 0.8;
        return clamp(n, 0.2, 1);
      })();

      const pickVoiceName = async () => {
        if (!globalThis.chrome?.tts?.getVoices) return '';
        const voices = await new Promise((resolve) => {
          try {
            chrome.tts.getVoices((v) => {
              if (chrome.runtime.lastError) {
                resolve([]);
                return;
              }
              resolve(Array.isArray(v) ? v : []);
            });
          } catch {
            resolve([]);
          }
        });

        if (!voices.length) return '';
        if (!normalizedLang) return typeof voices[0]?.voiceName === 'string' ? voices[0].voiceName : '';

        const l = normalizedLang;
        const candidates = voices
          .filter((v) => typeof v?.voiceName === 'string' && v.voiceName)
          .filter((v) => typeof v?.lang === 'string' && v.lang.toLowerCase() === l);

        const score = (voiceName) => {
          const n = String(voiceName || '').toLowerCase();
          if (n.includes('natural')) return 100;
          if (n.includes('neural')) return 90;
          if (n.includes('online')) return 80;
          if (n.includes('enhanced')) return 70;
          return 0;
        };

        if (candidates.length) {
          candidates.sort((a, b) => score(b.voiceName) - score(a.voiceName) || a.voiceName.localeCompare(b.voiceName));
          return candidates[0].voiceName;
        }

        const base = l.split('-')[0];
        if (base) {
          const prefixCandidates = voices
            .filter((v) => typeof v?.voiceName === 'string' && v.voiceName)
            .filter((v) => typeof v?.lang === 'string' && v.lang.toLowerCase().startsWith(`${base}-`));
          if (prefixCandidates.length) {
            prefixCandidates.sort(
              (a, b) => score(b.voiceName) - score(a.voiceName) || a.voiceName.localeCompare(b.voiceName)
            );
            return prefixCandidates[0].voiceName;
          }
        }

        return typeof voices[0]?.voiceName === 'string' ? voices[0].voiceName : '';
      };

      const voiceName = await pickVoiceName();

      return await new Promise((resolve) => {
        let settled = false;
        const events = [];
        const settle = (res) => {
          if (settled) return;
          settled = true;
          resolve({ voiceName: voiceName || '', lang: normalizedLang || '', ...res });
        };

        setTimeout(() => {
          settle({ ok: true, events });
        }, 2500);

        try {
          chrome.tts.stop();
        } catch {
          // ignore
        }

        try {
          chrome.tts.speak(
            t,
            {
              lang: normalizedLang || undefined,
              voiceName: voiceName || undefined,
              rate,
              pitch: 1,
              volume,
              requiredEventTypes: ['error'],
              desiredEventTypes: ['end', 'start'],
              onEvent: (e) => {
                if (!e || typeof e.type !== 'string') return;
                events.push({
                  type: e.type,
                  charIndex: typeof e.charIndex === 'number' ? e.charIndex : undefined,
                  errorMessage: typeof e.errorMessage === 'string' ? e.errorMessage : undefined,
                });
                if (e.type === 'error') {
                  try {
                    chrome.tts.stop();
                  } catch {
                    // ignore
                  }
                  settle({ ok: false, errorCode: E?.internal || 'INTERNAL', error: e.errorMessage || 'TTS error', events });
                  return;
                }
                if (e.type === 'end') {
                  settle({ ok: true, events });
                }
              },
            },
            () => {
              if (chrome.runtime.lastError) {
                settle({ ok: false, errorCode: E?.internal || 'INTERNAL', error: chrome.runtime.lastError.message, events });
              }
            }
          );
        } catch (err) {
          settle({ ok: false, errorCode: E?.internal || 'INTERNAL', error: err?.message || String(err), events });
        }
      });
    },

    async info() {
      try {
        const settings = await OM.settings.get();
        if (settings.enabled === false) return { ok: true, available: false, voiceCount: 0 };

        if (settings.ttsProvider === 'proxy') {
          const urlOk = typeof C?.DEFAULTS?.ttsProxyUrl === 'string' && C.DEFAULTS.ttsProxyUrl.trim();
          return {
            ok: true,
            available: !!urlOk,
            voiceCount: urlOk ? 1 : 0,
            provider: 'proxy',
          };
        }

        const available = !!globalThis.chrome?.tts?.speak && !!globalThis.chrome?.tts?.getVoices;
        if (!available) return { ok: true, available: false, voiceCount: 0 };

        const voices = await new Promise((resolve) => {
          try {
            chrome.tts.getVoices((v) => {
              if (chrome.runtime.lastError) {
                resolve([]);
                return;
              }
              resolve(Array.isArray(v) ? v : []);
            });
          } catch {
            resolve([]);
          }
        });

        const langs = voices.map((v) => (typeof v?.lang === 'string' ? v.lang : '')).filter(Boolean);
        return {
          ok: true,
          available: true,
          voiceCount: voices.length,
          sampleLangs: Array.from(new Set(langs)).slice(0, 6),
          provider: 'browser',
        };
      } catch (err) {
        return errRes(OM.messages?.ERROR_CODES?.internal || 'INTERNAL', err?.message || String(err));
      }
    },

    async stop() {
      try {
        if (globalThis.chrome?.tts?.stop) chrome.tts.stop();
        return { ok: true };
      } catch (err) {
        return errRes(OM.messages?.ERROR_CODES?.internal || 'INTERNAL', err?.message || String(err));
      }
    },
  });
})();

