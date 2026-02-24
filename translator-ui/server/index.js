const express = require('express');
const cors = require('cors');

const PORT = Number(process.env.PORT) || 8787;
const GOOGLE_TTS_API_KEY = process.env.GOOGLE_TTS_API_KEY || '';

const clamp = (n, min, max) => Math.min(max, Math.max(min, n));

const safeJson = async (r) => {
  try {
    return await r.json();
  } catch {
    return null;
  }
};

const app = express();
app.use(cors({ origin: true }));
app.use(express.json({ limit: '64kb' }));

app.get('/health', (_req, res) => {
  res.json({ ok: true });
});

app.post('/tts', async (req, res) => {
  try {
    if (!GOOGLE_TTS_API_KEY.trim()) {
      res.status(500).json({ ok: false, error: 'Server is missing GOOGLE_TTS_API_KEY' });
      return;
    }

    const text = typeof req.body?.text === 'string' ? req.body.text.trim() : '';
    if (!text) {
      res.status(400).json({ ok: false, error: 'Missing text' });
      return;
    }
    if (text.length > 500) {
      res.status(400).json({ ok: false, error: 'Text too long (max 500 chars)' });
      return;
    }

    const languageCode = typeof req.body?.languageCode === 'string' ? req.body.languageCode.trim() : '';
    const voiceName = typeof req.body?.voiceName === 'string' ? req.body.voiceName.trim() : '';

    const speakingRate = Number(req.body?.speakingRate);
    const pitch = Number(req.body?.pitch);

    const body = {
      input: { text },
      voice: {
        languageCode: languageCode || undefined,
        name: voiceName || undefined,
      },
      audioConfig: {
        audioEncoding: 'MP3',
        speakingRate: Number.isFinite(speakingRate) ? clamp(speakingRate, 0.25, 4.0) : undefined,
        pitch: Number.isFinite(pitch) ? clamp(pitch, -20, 20) : undefined,
      },
    };

    const url = `https://texttospeech.googleapis.com/v1/text:synthesize?key=${encodeURIComponent(
      GOOGLE_TTS_API_KEY.trim()
    )}`;

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
      res.status(502).json({ ok: false, error: msg });
      return;
    }

    const audioContent = typeof data?.audioContent === 'string' ? data.audioContent : '';
    if (!audioContent) {
      res.status(502).json({ ok: false, error: 'Google TTS returned empty audio' });
      return;
    }

    res.json({ ok: true, audio: { mimeType: 'audio/mpeg', base64: audioContent } });
  } catch (err) {
    res.status(500).json({ ok: false, error: err?.message || String(err) });
  }
});

app.listen(PORT, () => {
  console.log(`TTS proxy listening on http://localhost:${PORT}`);
});

