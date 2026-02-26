(() => {
  const OM = (globalThis.OM = globalThis.OM || {});
  const C = OM.constants;

  const formatRate = (n) => {
    const v = typeof n === 'number' ? n : Number(n);
    return Number.isFinite(v) ? v.toFixed(2) : '';
  };

  const formatVolume = (n) => {
    const v = typeof n === 'number' ? n : Number(n);
    return Number.isFinite(v) ? `${Math.round(v * 100)}%` : '';
  };

  const formatPitch = (n) => {
    const v = typeof n === 'number' ? n : Number(n);
    return Number.isFinite(v) ? String(v) : '';
  };

  const formatHoldDelay = (n) => {
    const ms = typeof n === 'number' ? n : Number(n);
    if (!Number.isFinite(ms)) return '';
    if (ms <= 0) return 'instant';
    if (ms < 1000) return `${Math.round(ms)}ms`;
    return `${(ms / 1000).toFixed(1)}s`;
  };

  const formatAutoCloseDelay = (n) => {
    const ms = typeof n === 'number' ? n : Number(n);
    if (!Number.isFinite(ms)) return '';
    if (ms <= 0) return 'off';
    if (ms < 1000) return `${Math.round(ms)}ms`;
    const s = ms / 1000;
    return Number.isInteger(s) ? `${s}s` : `${s.toFixed(1)}s`;
  };

  async function init() {
    const optionsSection = document.getElementById('optionsSection');
    const featuresSection = document.getElementById('featuresSection');

    if (optionsSection && featuresSection) {
      const syncAccordion = (opened, other) => {
        if (opened?.open) other.open = false;
      };
      optionsSection.addEventListener('toggle', () => syncAccordion(optionsSection, featuresSection));
      featuresSection.addEventListener('toggle', () => syncAccordion(featuresSection, optionsSection));
    }

    const sourceSelect = document.getElementById('sourceLangSelect');
    const targetSelect = document.getElementById('targetLangSelect');
    const emailInput = document.getElementById('emailInput');
    const enabledToggle = document.getElementById('enabledToggle');
    const holdToTranslateMsInput = document.getElementById('holdToTranslateMsInput');
    const holdToTranslateMsValue = document.getElementById('holdToTranslateMsValue');
    const resultAutoCloseMsInput = document.getElementById('resultAutoCloseMsInput');
    const resultAutoCloseMsValue = document.getElementById('resultAutoCloseMsValue');
    const ttsRateInput = document.getElementById('ttsRateInput');
    const ttsRateValue = document.getElementById('ttsRateValue');
    const ttsVolumeInput = document.getElementById('ttsVolumeInput');
    const ttsVolumeValue = document.getElementById('ttsVolumeValue');
    const ttsProviderSelect = document.getElementById('ttsProviderSelect');
    const proxySection = document.getElementById('proxyTtsSection');
    const ttsProxyUrlInput = document.getElementById('ttsProxyUrlInput');
    const googleLanguageCodeInput = document.getElementById('googleLanguageCodeInput');
    const googleVoiceNameInput = document.getElementById('googleVoiceNameInput');
    const googlePitchInput = document.getElementById('googlePitchInput');
    const googlePitchValue = document.getElementById('googlePitchValue');
    const verbFormsTextInput = document.getElementById('verbFormsTextInput');
    const verbFormsBtn = document.getElementById('verbFormsBtn');
    const verbFormsStatus = document.getElementById('verbFormsStatus');
    const verbFormsOutput = document.getElementById('verbFormsOutput');
    const explainTextInput = document.getElementById('explainTextInput');
    const explainContextInput = document.getElementById('explainContextInput');
    const explainSourceLangInput = document.getElementById('explainSourceLangInput');
    const explainInInput = document.getElementById('explainInInput');
    const explainBtn = document.getElementById('explainBtn');
    const explainStatus = document.getElementById('explainStatus');
    const explainOutput = document.getElementById('explainOutput');
    const openFeaturesWindowBtn = document.getElementById('openFeaturesWindowBtn');

    if (openFeaturesWindowBtn) {
      openFeaturesWindowBtn.addEventListener('click', async () => {
        const url = chrome.runtime.getURL('features.html');
        await chrome.windows.create({
          url,
          type: 'popup',
          width: 420,
          height: 720,
        });
      });
    }

    const settings = await OM.settings.get();

    if (enabledToggle) enabledToggle.checked = settings.enabled !== false;
    if (emailInput && settings.email) emailInput.value = settings.email;
    if (sourceSelect) sourceSelect.value = settings.sourceLang || C.DEFAULTS.sourceLang;
    if (targetSelect) targetSelect.value = settings.targetLang || C.DEFAULTS.targetLang;
    if (ttsProviderSelect) ttsProviderSelect.value = settings.ttsProvider || C.DEFAULTS.ttsProvider;

    if (holdToTranslateMsInput) {
      holdToTranslateMsInput.value = String(settings.holdToTranslateMs ?? C.DEFAULTS.holdToTranslateMs);
      if (holdToTranslateMsValue) holdToTranslateMsValue.textContent = formatHoldDelay(holdToTranslateMsInput.value);
    }

    if (resultAutoCloseMsInput) {
      resultAutoCloseMsInput.value = String(settings.resultAutoCloseMs ?? C.DEFAULTS.resultAutoCloseMs);
      if (resultAutoCloseMsValue) {
        resultAutoCloseMsValue.textContent = formatAutoCloseDelay(resultAutoCloseMsInput.value);
      }
    }

    if (ttsRateInput) {
      ttsRateInput.value = String(settings.ttsRate ?? C.DEFAULTS.ttsRate);
      if (ttsRateValue) ttsRateValue.textContent = formatRate(ttsRateInput.value);
    }
    if (ttsVolumeInput) {
      ttsVolumeInput.value = String(settings.ttsVolume ?? C.DEFAULTS.ttsVolume);
      if (ttsVolumeValue) ttsVolumeValue.textContent = formatVolume(ttsVolumeInput.value);
    }

    if (googleLanguageCodeInput) googleLanguageCodeInput.value = settings.googleLanguageCode || C.DEFAULTS.googleLanguageCode;
    if (googleVoiceNameInput) googleVoiceNameInput.value = settings.googleVoiceName || '';
    if (ttsProxyUrlInput) ttsProxyUrlInput.value = settings.ttsProxyUrl || C.DEFAULTS.ttsProxyUrl;
    if (googlePitchInput) {
      googlePitchInput.value = String(typeof settings.googlePitch === 'number' ? settings.googlePitch : C.DEFAULTS.googlePitch);
      if (googlePitchValue) googlePitchValue.textContent = formatPitch(googlePitchInput.value);
    }

    const syncProviderVisibility = () => {
      const provider = ttsProviderSelect ? String(ttsProviderSelect.value || '').toLowerCase() : 'browser';
      if (proxySection) proxySection.style.display = provider === 'proxy' ? '' : 'none';
    };
    syncProviderVisibility();

    if (enabledToggle) {
      enabledToggle.addEventListener('change', () => {
        OM.settings.setEnabled(enabledToggle.checked).catch(() => { });
      });
    }

    if (holdToTranslateMsInput) {
      const persistHoldDelay = async () => {
        await OM.settings.setHoldToTranslateMs(holdToTranslateMsInput.value);
      };
      holdToTranslateMsInput.addEventListener('input', () => {
        if (holdToTranslateMsValue) holdToTranslateMsValue.textContent = formatHoldDelay(holdToTranslateMsInput.value);
      });
      holdToTranslateMsInput.addEventListener('change', () => {
        persistHoldDelay().catch(() => { });
      });
    }

    if (resultAutoCloseMsInput) {
      const persistAutoClose = async () => {
        await OM.settings.setResultAutoCloseMs(resultAutoCloseMsInput.value);
      };
      resultAutoCloseMsInput.addEventListener('input', () => {
        if (resultAutoCloseMsValue) {
          resultAutoCloseMsValue.textContent = formatAutoCloseDelay(resultAutoCloseMsInput.value);
        }
      });
      resultAutoCloseMsInput.addEventListener('change', () => {
        persistAutoClose().catch(() => { });
      });
    }

    if (sourceSelect) {
      OM.popupSelectEnhancer.enhanceSelect(sourceSelect, {
        onChange: async (value) => {
          await OM.settings.setSourceLang(value);
        },
      });
    }

    if (targetSelect) {
      OM.popupSelectEnhancer.enhanceSelect(targetSelect, {
        onChange: async (value) => {
          await OM.settings.setTargetLang(value);
        },
      });
    }

    if (ttsProviderSelect) {
      OM.popupSelectEnhancer.enhanceSelect(ttsProviderSelect, {
        onChange: async (value) => {
          await OM.settings.setTtsProvider(value);
          syncProviderVisibility();
        },
      });
    }

    if (emailInput) {
      const persistEmail = async () => {
        await OM.settings.setEmail(emailInput.value);
      };

      emailInput.addEventListener('change', () => {
        persistEmail().catch(() => { });
      });
      emailInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') persistEmail().catch(() => { });
      });
    }

    if (ttsRateInput) {
      const persistRate = async () => {
        await OM.settings.setTtsRate(ttsRateInput.value);
      };
      ttsRateInput.addEventListener('input', () => {
        if (ttsRateValue) ttsRateValue.textContent = formatRate(ttsRateInput.value);
      });
      ttsRateInput.addEventListener('change', () => {
        persistRate().catch(() => { });
      });
    }

    if (ttsVolumeInput) {
      const persistVolume = async () => {
        await OM.settings.setTtsVolume(ttsVolumeInput.value);
      };
      ttsVolumeInput.addEventListener('input', () => {
        if (ttsVolumeValue) ttsVolumeValue.textContent = formatVolume(ttsVolumeInput.value);
      });
      ttsVolumeInput.addEventListener('change', () => {
        persistVolume().catch(() => { });
      });
    }

    if (googleLanguageCodeInput) {
      const persistLang = async () => {
        await OM.settings.setGoogleLanguageCode(googleLanguageCodeInput.value);
      };
      let langDebounceId;
      googleLanguageCodeInput.addEventListener('input', () => {
        clearTimeout(langDebounceId);
        langDebounceId = setTimeout(() => {
          persistLang().catch(() => { });
        }, 350);
      });
      googleLanguageCodeInput.addEventListener('change', () => {
        persistLang().catch(() => { });
      });
      googleLanguageCodeInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') persistLang().catch(() => { });
      });
    }

    if (googleVoiceNameInput) {
      const persistVoice = async () => {
        await OM.settings.setGoogleVoiceName(googleVoiceNameInput.value);
      };
      let voiceDebounceId;
      googleVoiceNameInput.addEventListener('input', () => {
        clearTimeout(voiceDebounceId);
        voiceDebounceId = setTimeout(() => {
          persistVoice().catch(() => { });
        }, 350);
      });
      googleVoiceNameInput.addEventListener('change', () => {
        persistVoice().catch(() => { });
      });
      googleVoiceNameInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') persistVoice().catch(() => { });
      });
    }

    if (googlePitchInput) {
      const persistPitch = async () => {
        await OM.settings.setGooglePitch(googlePitchInput.value);
      };
      googlePitchInput.addEventListener('input', () => {
        if (googlePitchValue) googlePitchValue.textContent = formatPitch(googlePitchInput.value);
      });
      googlePitchInput.addEventListener('change', () => {
        persistPitch().catch(() => { });
      });
    }

    if (explainSourceLangInput && !String(explainSourceLangInput.value || '').trim()) {
      explainSourceLangInput.value = String(settings.sourceLang || C.DEFAULTS.sourceLang || '').trim();
    }
    if (explainInInput && !String(explainInInput.value || '').trim()) {
      explainInInput.value = 'en';
    }

    if (ttsProxyUrlInput) {
      const persistProxyUrl = async () => {
        await OM.settings.setTtsProxyUrl(ttsProxyUrlInput.value);
      };
      let proxyDebounceId;
      ttsProxyUrlInput.addEventListener('input', () => {
        clearTimeout(proxyDebounceId);
        proxyDebounceId = setTimeout(() => {
          persistProxyUrl().catch(() => { });
        }, 350);
      });
      ttsProxyUrlInput.addEventListener('change', () => {
        persistProxyUrl().catch(() => { });
      });
      ttsProxyUrlInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') persistProxyUrl().catch(() => { });
      });
    }

    if (verbFormsBtn && verbFormsTextInput && verbFormsOutput) {
      const setStatus = (msg) => {
        if (verbFormsStatus) verbFormsStatus.textContent = msg || '';
      };

      const clearOutput = () => {
        verbFormsOutput.textContent = '';
        try {
          verbFormsOutput.replaceChildren();
        } catch {
          while (verbFormsOutput.firstChild) verbFormsOutput.removeChild(verbFormsOutput.firstChild);
        }
      };

      const setRawOutput = (v) => {
        const pre = document.createElement('pre');
        pre.textContent = String(v ?? '');
        verbFormsOutput.appendChild(pre);
      };

      const isVerbFormsResponse = (v) => {
        if (!v || typeof v !== 'object') return false;
        if (v.ok !== true) return false;
        return (
          typeof v.infinitive === 'string' &&
          typeof v.present === 'string' &&
          typeof v.past === 'string' &&
          typeof v.pastParticiple === 'string' &&
          typeof v.imperative === 'string'
        );
      };

      const renderVerbForms = (v) => {
        const header = document.createElement('div');
        header.className = 'verbFormsHeader';
        header.textContent = 'Verb forms';

        const grid = document.createElement('div');
        grid.className = 'verbFormsGrid';

        const addRow = (label, value) => {
          const l = document.createElement('div');
          l.className = 'verbFormsLabel';
          l.textContent = label;
          const val = document.createElement('div');
          val.className = 'verbFormsValue';
          val.textContent = value;
          grid.append(l, val);
        };

        const infinitive = String(v.infinitive || '').trim();
        addRow('Infinitive', infinitive ? `at ${infinitive}` : '');
        addRow('Present', String(v.present || '').trim());
        addRow('Past', String(v.past || '').trim());
        addRow('Past participle', String(v.pastParticiple || '').trim());
        addRow('Imperative', String(v.imperative || '').trim());

        verbFormsOutput.append(header, grid);
      };

      const setOutput = (v) => {
        clearOutput();
        if (v == null) return;

        if (typeof v === 'string') {
          setRawOutput(v);
          return;
        }

        if (isVerbFormsResponse(v)) {
          renderVerbForms(v);
          return;
        }

        try {
          setRawOutput(JSON.stringify(v, null, 2));
        } catch {
          setRawOutput(String(v));
        }
      };

      const callVerbForms = async () => {
        const text = String(verbFormsTextInput.value || '').trim();
        if (!text) {
          setStatus('Type something first.');
          setOutput(null);
          return;
        }

        verbFormsBtn.disabled = true;
        setStatus('Loading…');
        setOutput(null);

        const url = C.DEFAULTS.verbFormsProxyUrl;
        const r = await fetch(url, {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify({ text }),
        });

        const d = await (async () => {
          try {
            return await r.json();
          } catch {
            return null;
          }
        })();

        if (!r.ok) {
          const statusText = r.status ? `HTTP ${r.status}` : 'HTTP error';
          const msg = typeof d?.error === 'string' && d.error.trim() ? d.error.trim() : statusText;
          throw new Error(msg);
        }

        setStatus('Done.');
        setOutput(d ?? (await r.text()));
      };

      verbFormsBtn.addEventListener('click', () => {
        callVerbForms()
          .catch((e) => {
            setStatus(e?.message ? String(e.message) : 'Request failed');
          })
          .finally(() => {
            verbFormsBtn.disabled = false;
          });
      });

      verbFormsTextInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) verbFormsBtn.click();
      });
    }

    if (explainBtn && explainTextInput && explainOutput) {
      const setStatus = (msg) => {
        if (explainStatus) explainStatus.textContent = msg || '';
      };

      const clearOutput = () => {
        explainOutput.textContent = '';
        try {
          explainOutput.replaceChildren();
        } catch {
          while (explainOutput.firstChild) explainOutput.removeChild(explainOutput.firstChild);
        }
      };

      const setRawOutput = (v) => {
        const pre = document.createElement('pre');
        pre.textContent = String(v ?? '');
        explainOutput.appendChild(pre);
      };

      const isExplainResponse = (v) => {
        if (!v || typeof v !== 'object') return false;
        if (v.ok !== true) return false;
        return (
          typeof v.summary === 'string' &&
          typeof v.what === 'string' &&
          typeof v.when === 'string' &&
          typeof v.why === 'string' &&
          Array.isArray(v.notes) &&
          Array.isArray(v.alternatives) &&
          Array.isArray(v.examples)
        );
      };

      const addSection = (title, text) => {
        const section = document.createElement('div');
        section.className = 'explainSection';

        const t = document.createElement('div');
        t.className = 'explainSectionTitle';
        t.textContent = title;

        const p = document.createElement('div');
        p.className = 'explainText';
        p.textContent = String(text || '').trim();

        section.append(t, p);
        return section;
      };

      const addListSection = (title, items) => {
        const section = document.createElement('div');
        section.className = 'explainSection';

        const t = document.createElement('div');
        t.className = 'explainSectionTitle';
        t.textContent = title;

        const ul = document.createElement('ul');
        ul.className = 'explainList';
        for (const raw of items || []) {
          const s = String(raw || '').trim();
          if (!s) continue;
          const li = document.createElement('li');
          li.textContent = s;
          ul.appendChild(li);
        }

        section.append(t, ul);
        return section;
      };

      const addExamplesSection = (title, examples) => {
        const section = document.createElement('div');
        section.className = 'explainSection';

        const t = document.createElement('div');
        t.className = 'explainSectionTitle';
        t.textContent = title;

        const wrap = document.createElement('div');
        wrap.className = 'explainExamples';

        for (const ex of examples || []) {
          const src = String(ex?.source || '').trim();
          const meaning = String(ex?.meaning || '').trim();
          if (!src || !meaning) continue;

          const card = document.createElement('div');
          card.className = 'explainExample';

          const s = document.createElement('div');
          s.className = 'explainExampleSource';
          s.textContent = src;

          const m = document.createElement('div');
          m.className = 'explainExampleMeaning';
          m.textContent = meaning;

          card.append(s, m);
          wrap.appendChild(card);
        }

        section.append(t, wrap);
        return section;
      };

      const renderExplain = (v) => {
        const header = document.createElement('div');
        header.className = 'explainHeader';
        header.textContent = 'Explanation';

        explainOutput.appendChild(header);
        explainOutput.appendChild(addSection('Summary', v.summary));
        explainOutput.appendChild(addSection('What', v.what));
        explainOutput.appendChild(addSection('When', v.when));
        explainOutput.appendChild(addSection('Why', v.why));

        if (Array.isArray(v.notes) && v.notes.some((x) => String(x || '').trim())) {
          explainOutput.appendChild(addListSection('Notes', v.notes));
        }
        if (Array.isArray(v.alternatives) && v.alternatives.some((x) => String(x || '').trim())) {
          explainOutput.appendChild(addListSection('Alternatives', v.alternatives));
        }
        if (Array.isArray(v.examples) && v.examples.some((x) => String(x?.source || '').trim())) {
          explainOutput.appendChild(addExamplesSection('Examples', v.examples));
        }
      };

      const setOutput = (v) => {
        clearOutput();
        if (v == null) return;

        if (typeof v === 'string') {
          setRawOutput(v);
          return;
        }

        if (isExplainResponse(v)) {
          renderExplain(v);
          return;
        }

        try {
          setRawOutput(JSON.stringify(v, null, 2));
        } catch {
          setRawOutput(String(v));
        }
      };

      const callExplain = async () => {
        const text = String(explainTextInput.value || '').trim();
        const context = String(explainContextInput?.value || '').trim();
        const sourceLang = String(explainSourceLangInput?.value || '').trim();
        const explainIn = String(explainInInput?.value || '').trim() || 'en';

        if (!text) {
          setStatus('Type something first.');
          setOutput(null);
          return;
        }

        explainBtn.disabled = true;
        setStatus('Loading…');
        setOutput(null);

        const url = C.DEFAULTS.explainProxyUrl;
        const r = await fetch(url, {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify({
            text,
            context: context || null,
            sourceLang: sourceLang || null,
            explainIn,
          }),
        });

        const d = await (async () => {
          try {
            return await r.json();
          } catch {
            return null;
          }
        })();

        if (!r.ok) {
          const statusText = r.status ? `HTTP ${r.status}` : 'HTTP error';
          const msg = typeof d?.error === 'string' && d.error.trim() ? d.error.trim() : statusText;
          throw new Error(msg);
        }

        setStatus('Done.');
        setOutput(d ?? (await r.text()));
      };

      explainBtn.addEventListener('click', () => {
        callExplain()
          .catch((e) => {
            setStatus(e?.message ? String(e.message) : 'Request failed');
          })
          .finally(() => {
            explainBtn.disabled = false;
          });
      });

      explainTextInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) explainBtn.click();
      });
    }
  }

  OM.popupController = Object.freeze({ init });
})();

