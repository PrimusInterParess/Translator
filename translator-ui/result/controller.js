(() => {
  // kari e noremrgkvc gwejfdc
  const OM = (globalThis.OM = globalThis.OM || {});
  const C = OM.constants;

  let activeAudio = null;
  const stopAudio = () => {
    try {
      if (activeAudio) {
        activeAudio.pause();
        activeAudio.src = '';
      }
    } catch {
      // ignore
    } finally {
      activeAudio = null;
    }
  };
  const playBase64Audio = ({ mimeType, base64, volume } = {}) => {
    const mt = typeof mimeType === 'string' ? mimeType : '';
    const b64 = typeof base64 === 'string' ? base64 : '';
    if (!mt || !b64) return { ok: false, error: 'Missing audio' };

    stopAudio();
    const a = new Audio(`data:${mt};base64,${b64}`);
    const v = typeof volume === 'number' ? volume : Number(volume);
    if (Number.isFinite(v)) a.volume = Math.min(1, Math.max(0, v));
    activeAudio = a;

    try {
      const p = a.play();
      if (p && typeof p.catch === 'function') p.catch(() => {});
      return { ok: true };
    } catch (err) {
      stopAudio();
      return { ok: false, error: err?.message || String(err) };
    }
  };

  function renderRecord({ record, textInput, output, statusEl, translateBtn }) {
    if (record?.text) textInput.value = record.text;

    output.classList.remove('err');
    statusEl.textContent = '';

    if (record?.status === 'translating') {
      OM.ui.setLoading(output, true);
      output.textContent = 'Translating';
      if (translateBtn) translateBtn.disabled = true;
      return;
    }

    OM.ui.setLoading(output, false);
    if (translateBtn) translateBtn.disabled = false;

    if (record?.error) {
      output.textContent = OM.errors.friendlyTranslateError(record, output);
      output.classList.add('err');
      return;
    }

    if (typeof record?.translatedText === 'string' && record.translatedText) {
      output.textContent = record.translatedText;
      return;
    }

    output.textContent = record?.text ? 'Click Translate.' : 'Enter text to translate.';
  }

  async function loadInitialRecord({ requestId, textInput, output, statusEl, translateBtn }) {
    // Prefer per-request record.
    if (requestId) {
      const record = await OM.translationsRepo.get(requestId);
      if (record) {
        renderRecord({ record, textInput, output, statusEl, translateBtn });
        return record;
      }
    }

    // Fallback: legacy global lastTranslation (back-compat).
    const legacy = await OM.storage.get([C.STORAGE_KEYS.lastTranslation]);
    const lastTranslation = legacy[C.STORAGE_KEYS.lastTranslation];
    renderRecord({ record: lastTranslation || null, textInput, output, statusEl, translateBtn });
    return lastTranslation || null;
  }

  async function translate({ requestId, onRequestId, textInput, output, statusEl, translateBtn }) {
    const raw = textInput.value || '';
    const text = OM.text.sanitize(raw);

    if (!text) {
      OM.ui.setLoading(output, false);
      output.classList.remove('err');
      output.textContent = 'Enter text to translate.';
      statusEl.textContent = '';
      return;
    }
    if (OM.text.isTooLong(raw)) {
      OM.ui.setLoading(output, false);
      output.textContent = `Text is too long (max ${C.MAX_TEXT_LEN} chars).`;
      output.classList.add('err');
      statusEl.textContent = '';
      return;
    }

    output.classList.remove('err');
    statusEl.textContent = '';

    OM.ui.setLoading(output, true);
    output.textContent = 'Translating';
    if (translateBtn) translateBtn.disabled = true;

    try {
      const guessedSource = OM.lang.guessSourceLang(text);
      const result = await OM.runtime.sendMessage({ type: 'translate', text, guessedSource, requestId });

      if (result?.ok && typeof result.translatedText === 'string') {
        if (typeof result.requestId === 'string' && result.requestId && typeof onRequestId === 'function') {
          onRequestId(result.requestId);
        }
        output.textContent = result.translatedText;
      } else {
        output.textContent = OM.errors.friendlyTranslateError(result, output);
        output.classList.add('err');
      }
    } catch (err) {
      output.textContent = OM.errors.friendlyTranslateError(err, output);
      output.classList.add('err');
    } finally {
      OM.ui.setLoading(output, false);
      if (translateBtn) translateBtn.disabled = false;
    }
  }

  OM.resultController = Object.freeze({
    init() {
      const textInput = document.getElementById('textInput');
      const translateBtn = document.getElementById('translateBtn');
      const speakBtn = document.getElementById('speakBtn');
      const output = document.getElementById('output');
      const statusEl = document.getElementById('status');

      let ttsAvailable = false;
      let ttsVoiceCount = 0;
      let ttsProvider = '';
      let lastDetectedSourceLang = '';
      let autoCloseTimerId = null;

      const normalizeAutoCloseMs = (v) => {
        const n = typeof v === 'number' ? v : Number(v);
        const fallback = C.DEFAULTS?.resultAutoCloseMs ?? 25000;
        const base = Number.isFinite(n) ? Math.round(n) : fallback;
        return Math.min(300000, Math.max(0, base));
      };

      const closeSelf = () => {
        try {
          OM.runtime.sendMessage({ type: 'closeResultWindow' });
        } catch {
          // ignore
        }
        try {
          window.close();
        } catch {
          // ignore
        }
      };

      const clearAutoClose = () => {
        if (autoCloseTimerId != null) {
          clearTimeout(autoCloseTimerId);
          autoCloseTimerId = null;
        }
      };

      const scheduleAutoClose = (ms) => {
        clearAutoClose();
        const delay = normalizeAutoCloseMs(ms);
        if (delay <= 0) return;
        autoCloseTimerId = setTimeout(closeSelf, delay);
      };

      const urlParams = (() => {
        try {
          return new URLSearchParams(globalThis.location?.search || '');
        } catch {
          return new URLSearchParams();
        }
      })();

      let requestId = (() => {
        const id = urlParams.get('id');
        return typeof id === 'string' ? id : '';
      })();

      const setRequestId = (id) => {
        const next = typeof id === 'string' ? id : '';
        if (!next || next === requestId) return;
        requestId = next;
        try {
          const u = new URL(globalThis.location.href);
          u.searchParams.set('id', requestId);
          globalThis.history.replaceState(null, '', u.toString());
        } catch {
          // ignore
        }
      };

      OM.icons?.applySpeakerIconToButton?.(speakBtn);

      scheduleAutoClose(C.DEFAULTS?.resultAutoCloseMs ?? 25000);
      OM.settings
        ?.get?.()
        .then((settings) => {
          scheduleAutoClose(settings?.resultAutoCloseMs);
        })
        .catch(() => {});

      translateBtn.addEventListener('click', () => {
        translate({ requestId, onRequestId: setRequestId, textInput, output, statusEl, translateBtn });
      });

      if (speakBtn) {
        if (!OM.speech?.isSupported?.()) {
          speakBtn.disabled = true;
          speakBtn.title = 'Listen';
        }

        OM.runtime
          .sendMessage({ type: 'ttsInfo' })
          .then((info) => {
            if (!info?.ok) return;
            ttsAvailable = info.available === true;
            ttsVoiceCount = typeof info.voiceCount === 'number' ? info.voiceCount : 0;
            ttsProvider = typeof info.provider === 'string' ? info.provider : '';
            speakBtn.title = 'Listen';
            speakBtn.disabled = !(ttsAvailable && ttsVoiceCount > 0);
          })
          .catch(() => {});

        speakBtn.addEventListener('click', async () => {
          if (speakBtn.disabled) return;
          const raw = textInput.value || '';
          const t = OM.text.sanitize(raw);
          if (!t) return;
          const lang = lastDetectedSourceLang || OM.ttsPrefs.guessSpeakLang(t);
          const prefs = await OM.ttsPrefs.getSpeechPrefs();
          speakBtn.disabled = true;

          if (!ttsAvailable) {
            speakBtn.disabled = false;
            return;
          }
          if (ttsVoiceCount <= 0) {
            speakBtn.disabled = false;
            return;
          }

          stopAudio();
          try {
            await OM.runtime.sendMessage({ type: 'ttsStop' });
          } catch {
            // ignore
          }
          try {
            OM.speech?.stop?.();
          } catch {
            // ignore
          }

          let res = await OM.runtime.sendMessage({ type: 'ttsSpeak', text: t, lang });
          if (res?.ok && res?.audio?.base64) {
            playBase64Audio({ ...res.audio, volume: prefs.volume });
          }
          if (!res?.ok) {
            const msg = typeof res?.error === 'string' && res.error ? res.error : 'TTS failed';
            statusEl.textContent = ttsProvider === 'google' ? `Google TTS failed: ${msg}` : `TTS failed: ${msg}`;
            setTimeout(() => {
              if (statusEl.textContent.includes(msg)) statusEl.textContent = '';
            }, 5000);
            // Only fallback when chrome.tts is truly unavailable; otherwise avoid overlap/echo.
            const err = typeof res?.error === 'string' ? res.error.toLowerCase() : '';
            if (err.includes('chrome.tts not available')) {
              res = await OM.speech?.speak?.({
                text: t,
                lang,
                rate: prefs.rate,
                volume: prefs.volume,
              });
            }
          }

          // Intentionally no UI debug/status messages for speech.
          speakBtn.disabled = false;
        });
      }

      textInput.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
          translate({ requestId, onRequestId: setRequestId, textInput, output, statusEl, translateBtn });
        }
      });

      // Initial paint: show something immediately, then sync from storage.
      output.classList.remove('err');
      statusEl.textContent = '';
      OM.ui.setLoading(output, true);
      output.textContent = 'Loading';
      translateBtn.disabled = true;

      (async () => {
        if (!requestId) {
          const lastId = await OM.translationsRepo.getLastId();
          if (lastId) setRequestId(lastId);
        }
        const record = await loadInitialRecord({ requestId, textInput, output, statusEl, translateBtn });
        if (typeof record?.source === 'string' && record.source) {
          lastDetectedSourceLang = record.source;
        }
      })().catch(() => {
        OM.ui.setLoading(output, false);
        output.textContent = 'Enter text to translate.';
        translateBtn.disabled = false;
      });

      if (globalThis.chrome?.storage?.onChanged?.addListener) {
        chrome.storage.onChanged.addListener((changes, areaName) => {
          if (areaName !== 'local') return;

          const autoCloseCh = changes?.[C.STORAGE_KEYS.resultAutoCloseMs];
          if (autoCloseCh) scheduleAutoClose(autoCloseCh.newValue);

          // If this window has a requestId, listen for updates to that record.
          if (requestId) {
            const k = OM.translationsRepo.keyForId(requestId);
            const ch = changes[k];
            if (!ch) return;
            if (typeof ch.newValue?.source === 'string' && ch.newValue.source) {
              lastDetectedSourceLang = ch.newValue.source;
            }
            renderRecord({
              record: ch.newValue || null,
              textInput,
              output,
              statusEl,
              translateBtn,
            });
            return;
          }

          // Otherwise, fall back to legacy updates.
          const legacyCh = changes[C.STORAGE_KEYS.lastTranslation];
          if (!legacyCh) return;
          if (typeof legacyCh.newValue?.source === 'string' && legacyCh.newValue.source) {
            lastDetectedSourceLang = legacyCh.newValue.source;
          }
          renderRecord({
            record: legacyCh.newValue || null,
            textInput,
            output,
            statusEl,
            translateBtn,
          });
        });
      }
    },
  });
})();

