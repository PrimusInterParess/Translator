(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const errRes = (errorCode, error) => ({ ok: false, errorCode, error });
  const clamp = (n, min, max) => Math.min(max, Math.max(min, n));

  const safeJson = async (r) => {
    try {
      return await r.json();
    } catch {
      return null;
    }
  };

  OM.googleTtsService = Object.freeze({
    async synthesize({ text, apiKey, languageCode, voiceName, speakingRate, pitch } = {}) {
      const E = OM.messages?.ERROR_CODES;

      const t = typeof text === 'string' ? text.trim() : '';
      if (!t) return errRes(E?.missingText || 'MISSING_TEXT', 'Missing text');

      const key = typeof apiKey === 'string' ? apiKey.trim() : '';
      if (!key) return errRes(E?.internal || 'INTERNAL', 'Google TTS API key is missing');

      const lang = typeof languageCode === 'string' ? languageCode.trim() : '';
      const vn = typeof voiceName === 'string' ? voiceName.trim() : '';

      const rateNum = typeof speakingRate === 'number' ? speakingRate : Number(speakingRate);
      const pitchNum = typeof pitch === 'number' ? pitch : Number(pitch);

      const body = {
        input: { text: t },
        voice: {
          languageCode: lang || undefined,
          name: vn || undefined,
        },
        audioConfig: {
          audioEncoding: 'MP3',
          speakingRate: Number.isFinite(rateNum) ? clamp(rateNum, 0.25, 4.0) : undefined,
          pitch: Number.isFinite(pitchNum) ? clamp(pitchNum, -20, 20) : undefined,
        },
      };

      try {
        const url = `https://texttospeech.googleapis.com/v1/text:synthesize?key=${encodeURIComponent(key)}`;
        const r = await fetch(url, {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify(body),
        });

        const data = await safeJson(r);
        if (!r.ok) {
          const msg =
            typeof data?.error?.message === 'string'
              ? data.error.message
              : typeof data?.message === 'string'
                ? data.message
                : r.status
                  ? `HTTP ${r.status}`
                  : 'Google TTS error';
          return errRes(E?.internal || 'INTERNAL', msg);
        }

        const audioContent = typeof data?.audioContent === 'string' ? data.audioContent : '';
        if (!audioContent) return errRes(E?.internal || 'INTERNAL', 'Google TTS returned empty audio');

        return { ok: true, audio: { mimeType: 'audio/mpeg', base64: audioContent } };
      } catch (err) {
        return errRes(E?.internal || 'INTERNAL', err?.message || String(err));
      }
    },
  });
})();

