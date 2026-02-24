(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const POPUP_ID = 'trans-popup';
  const APP_NAME = 'OversætMig';

  const getExisting = () => document.getElementById(POPUP_ID);
  const removeExisting = () => {
    const p = getExisting();
    if (p) p.remove();
  };

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
      if (p && typeof p.catch === 'function') p.catch(() => { });
      return { ok: true };
    } catch (err) {
      stopAudio();
      return { ok: false, error: err?.message || String(err) };
    }
  };

  const createButton = () => {
    const closeBtn = document.createElement('button');
    closeBtn.type = 'button';
    closeBtn.className = 'trans-close';
    closeBtn.setAttribute('aria-label', 'Close');
    closeBtn.textContent = '×';
    return closeBtn;
  };

  const createSpeakButton = () => {
    const speakBtn = document.createElement('button');
    speakBtn.type = 'button';
    speakBtn.className = 'trans-speak';
    speakBtn.setAttribute('aria-label', 'Listen');
    speakBtn.title = 'Listen';
    return speakBtn;
  };

  const createTitle = () => {
    const title = document.createElement('div');
    title.className = 'trans-title';
    title.textContent = APP_NAME;
    return title;
  };

  const createInput = (text) => {
    const input = document.createElement('textarea');
    input.className = 'trans-input';
    input.value = text;
    return input;
  };

  const createOutput = () => {
    const output = document.createElement('div');
    output.className = 'trans-output';
    output.textContent = '';
    return output;
  };

  async function translateText(text, outputEl, onMeta) {
    const t = OM.text.sanitize(text);
    if (!t) {
      OM.ui.setLoading(outputEl, false);
      outputEl.textContent = 'Enter text to translate.';
      return;
    }
    if (OM.text.isTooLong(text)) {
      OM.ui.setLoading(outputEl, false);
      outputEl.textContent = `Text is too long (max ${OM.constants.MAX_TEXT_LEN} chars).`;
      return;
    }

    OM.ui.setLoading(outputEl, true);
    outputEl.textContent = 'Translating';
    try {
      const guessedSource = OM.lang.guessSourceLang(t);
      const result = await OM.runtime.sendMessage({ type: 'translate', text: t, guessedSource });
      if (result && result.ok && typeof result.translatedText === 'string') {
        outputEl.textContent = result.translatedText;
        if (typeof onMeta === 'function') onMeta({ source: result.source, target: result.target });
      } else {
        outputEl.textContent = OM.errors.friendlyTranslateError(result, outputEl);
      }
    } catch (err) {
      outputEl.textContent = OM.errors.friendlyTranslateError(err, outputEl);
    } finally {
      OM.ui.setLoading(outputEl, false);
    }
  }

  OM.contentPopup = Object.freeze({
    close() {
      removeExisting();
    },
    openAt({ x, y, text }) {
      removeExisting();

      const popup = document.createElement('div');
      popup.id = POPUP_ID;
      popup.style.left = `${x}px`;
      popup.style.top = `${y}px`;

      let lastDetectedSourceLang = '';
      const translateAndTrack = (value) =>
        translateText(value, output, (meta) => {
          const s = typeof meta?.source === 'string' ? meta.source : '';
          lastDetectedSourceLang = s;
        });

      const closeBtn = createButton();
      const speakBtn = createSpeakButton();
      const actions = document.createElement('div');
      actions.className = 'trans-actions';
      const title = createTitle();
      const input = createInput(text);
      const output = createOutput();

      popup.appendChild(closeBtn);
      popup.appendChild(title);
      popup.appendChild(input);
      actions.appendChild(speakBtn);
      popup.appendChild(actions);
      popup.appendChild(output);
      document.body.appendChild(popup);

      OM.icons?.applySpeakerIconToButton?.(speakBtn);

      const stop = (ev) => ev.stopPropagation();
      closeBtn.addEventListener('mousedown', stop);
      closeBtn.addEventListener('mouseup', stop);
      closeBtn.addEventListener('click', (ev) => {
        stop(ev);
        stopAudio();
        popup.remove();
      });

      let ttsAvailable = false;
      let ttsVoiceCount = 0;
      let ttsProvider = '';
      OM.runtime
        .sendMessage({ type: 'ttsInfo' })
        .then((info) => {
          if (!info?.ok) return;
          if (info.available !== true) {
            speakBtn.disabled = true;
          } else if (typeof info.voiceCount === 'number' && info.voiceCount <= 0) {
            speakBtn.disabled = true;
          } else {
            speakBtn.disabled = false;
          }
          ttsAvailable = info.available === true;
          ttsVoiceCount = typeof info.voiceCount === 'number' ? info.voiceCount : 0;
          ttsProvider = typeof info.provider === 'string' ? info.provider : '';
        })
        .catch(() => { });
      speakBtn.addEventListener('mousedown', stop);
      speakBtn.addEventListener('mouseup', stop);
      speakBtn.addEventListener('click', async (ev) => {
        stop(ev);
        if (speakBtn.disabled) return;
        const raw = input.value || '';
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
          speakBtn.title = ttsProvider === 'google' ? `Google TTS failed: ${msg}` : `TTS failed: ${msg}`;
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

      let debounceId;
      input.addEventListener('input', () => {
        clearTimeout(debounceId);
        debounceId = setTimeout(() => {
          translateAndTrack(input.value || '');
        }, 500);
      });

      translateAndTrack(input.value || '');
    },

    isInsideEventTarget(target) {
      const p = getExisting();
      return !!(p && target && p.contains(target));
    },

    closeIfClickOutside(target) {
      const p = getExisting();
      if (p && target && !p.contains(target)) p.remove();
    },
  });
})();

