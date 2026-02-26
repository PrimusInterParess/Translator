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

    const settings = await OM.settings.get();

    if (enabledToggle) enabledToggle.checked = settings.enabled !== false;
    if (settings.email) emailInput.value = settings.email;
    sourceSelect.value = settings.sourceLang || C.DEFAULTS.sourceLang;
    targetSelect.value = settings.targetLang || C.DEFAULTS.targetLang;
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
        OM.settings.setEnabled(enabledToggle.checked).catch(() => {});
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
        persistHoldDelay().catch(() => {});
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
        persistAutoClose().catch(() => {});
      });
    }

    OM.popupSelectEnhancer.enhanceSelect(sourceSelect, {
      onChange: async (value) => {
        await OM.settings.setSourceLang(value);
      },
    });

    OM.popupSelectEnhancer.enhanceSelect(targetSelect, {
      onChange: async (value) => {
        await OM.settings.setTargetLang(value);
      },
    });

    if (ttsProviderSelect) {
      OM.popupSelectEnhancer.enhanceSelect(ttsProviderSelect, {
        onChange: async (value) => {
          await OM.settings.setTtsProvider(value);
          syncProviderVisibility();
        },
      });
    }

    const persistEmail = async () => {
      await OM.settings.setEmail(emailInput.value);
    };

    emailInput.addEventListener('change', () => {
      persistEmail().catch(() => {});
    });
    emailInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') persistEmail().catch(() => {});
    });

    if (ttsRateInput) {
      const persistRate = async () => {
        await OM.settings.setTtsRate(ttsRateInput.value);
      };
      ttsRateInput.addEventListener('input', () => {
        if (ttsRateValue) ttsRateValue.textContent = formatRate(ttsRateInput.value);
      });
      ttsRateInput.addEventListener('change', () => {
        persistRate().catch(() => {});
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
        persistVolume().catch(() => {});
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
          persistLang().catch(() => {});
        }, 350);
      });
      googleLanguageCodeInput.addEventListener('change', () => {
        persistLang().catch(() => {});
      });
      googleLanguageCodeInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') persistLang().catch(() => {});
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
          persistVoice().catch(() => {});
        }, 350);
      });
      googleVoiceNameInput.addEventListener('change', () => {
        persistVoice().catch(() => {});
      });
      googleVoiceNameInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') persistVoice().catch(() => {});
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
        persistPitch().catch(() => {});
      });
    }

    if (ttsProxyUrlInput) {
      const persistProxyUrl = async () => {
        await OM.settings.setTtsProxyUrl(ttsProxyUrlInput.value);
      };
      let proxyDebounceId;
      ttsProxyUrlInput.addEventListener('input', () => {
        clearTimeout(proxyDebounceId);
        proxyDebounceId = setTimeout(() => {
          persistProxyUrl().catch(() => {});
        }, 350);
      });
      ttsProxyUrlInput.addEventListener('change', () => {
        persistProxyUrl().catch(() => {});
      });
      ttsProxyUrlInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') persistProxyUrl().catch(() => {});
      });
    }
  }

  OM.popupController = Object.freeze({ init });
})();

