(() => {
  const OM = (globalThis.OM = globalThis.OM || {});
  const C = OM.constants;
  const M = OM.messages;

  // Serialize context-menu changes to avoid duplicate create() races.
  let contextMenuSyncChain = Promise.resolve();

  const removeTranslateContextMenu = () =>
    new Promise((resolve) => {
      try {
        chrome.contextMenus.remove(C.CONTEXT_MENU_ID, () => {
          // Prevent "Unchecked runtime.lastError" spam when item doesn't exist.
          void chrome.runtime?.lastError;
          resolve();
        });
      } catch {
        resolve();
      }
    });

  const createTranslateContextMenu = () =>
    new Promise((resolve) => {
      try {
        chrome.contextMenus.create(
          {
            id: C.CONTEXT_MENU_ID,
            title: 'OversætMig: Translate selection',
            contexts: ['selection'],
          },
          () => {
            // Prevent "Unchecked runtime.lastError" spam (e.g. duplicate id during edge cases).
            void chrome.runtime?.lastError;
            resolve();
          }
        );
      } catch {
        resolve();
      }
    });

  async function syncContextMenu(enabled) {
    if (!globalThis.chrome?.contextMenus) return;

    // Chain updates to prevent parallel create/remove.
    contextMenuSyncChain = contextMenuSyncChain
      .catch(() => {})
      .then(async () => {
        const isOn = enabled !== false;

        if (!isOn) {
          await removeTranslateContextMenu();
          return;
        }

        // Recreate deterministically (remove ignores "not found").
        await removeTranslateContextMenu();
        await createTranslateContextMenu();
      });

    await contextMenuSyncChain;
  }

  const normalizeErrMsg = (err) => {
    if (!err) return '';
    if (typeof err === 'string') return err.trim();
    if (typeof err?.message === 'string') return err.message.trim();
    return String(err).trim();
  };

  const classifyErrorCode = (err) => {
    const msg = normalizeErrMsg(err);
    const lower = msg.toLowerCase();
    if (!msg) return M?.ERROR_CODES?.internal || 'INTERNAL';
    if (lower.includes('failed to fetch') || lower.includes('networkerror') || lower.includes('http ')) {
      return M?.ERROR_CODES?.network || 'NETWORK';
    }
    return M?.ERROR_CODES?.internal || 'INTERNAL';
  };

  const errRes = (errorCode, error) => ({ ok: false, errorCode, error });

  OM.backgroundHandlers = Object.freeze({
    async onInstalled() {
      let enabled = true;
      try {
        const settings = await OM.settings.get();
        enabled = settings.enabled !== false;
      } catch {
        enabled = true;
      }

      await syncContextMenu(enabled);
    },

    async syncContextMenu(enabled) {
      await syncContextMenu(enabled);
    },

    async onContextMenuClick(info) {
      if (info.menuItemId !== C.CONTEXT_MENU_ID) return;

      const settings = await OM.settings.get();
      if (settings.enabled === false) return;

      const raw = typeof info.selectionText === 'string' ? info.selectionText : '';
      const text = OM.text.sanitize(raw);
      if (!text) return;

      const guessedSource = OM.lang.guessSourceLang(text);

      try {
        const requestId = OM.translationsRepo.createId();

        // Open the result window immediately with a loader, then update it via storage when ready.
        await OM.translationsRepo.set(requestId, {
          id: requestId,
          text,
          translatedText: '',
          source: '',
          target: '',
          status: 'translating',
          at: Date.now(),
        });

        await OM.resultWindow.open(requestId);

        const res = await OM.translatorService.translateFromMessage({ rawText: text, guessedSource });
        if (!res?.ok) {
          await OM.translationsRepo.update(requestId, {
            text,
            translatedText: '',
            source: '',
            target: '',
            status: 'error',
            error: res?.error || 'Translate failed',
            errorCode: res?.errorCode || M?.ERROR_CODES?.internal || 'INTERNAL',
            at: Date.now(),
          });
          return;
        }

        await OM.translationsRepo.update(requestId, {
          text: res.text,
          translatedText: res.translatedText,
          source: res.source,
          target: res.target,
          status: 'done',
          at: Date.now(),
        });
      } catch (err) {
        // If we fail before creating a requestId, best-effort write legacy key for visibility.
        try {
          await OM.storage.set({
            [C.STORAGE_KEYS.lastTranslation]: {
              text,
              translatedText: '',
              source: '',
              target: '',
              status: 'error',
              error: err?.message || String(err),
              errorCode: M?.ERROR_CODES?.internal || 'INTERNAL',
              at: Date.now(),
            },
          });
        } catch {
          // ignore
        }
      }
    },

    async onMessage(msg, sender) {
      if (!msg || typeof msg !== 'object') {
        return errRes(M?.ERROR_CODES?.invalidMessage || 'INVALID_MESSAGE', 'Invalid message');
      }

      const type = msg.type;
      if (typeof type !== 'string' || !type) {
        return errRes(M?.ERROR_CODES?.invalidMessage || 'INVALID_MESSAGE', 'Invalid message');
      }

      const handlers = {
        [M?.TYPES?.getSettings || 'getSettings']: async () => {
          const settings = await OM.settings.get();
          return { ok: true, ...settings };
        },

        [M?.TYPES?.closeResultWindow || 'closeResultWindow']: async () => {
          const winId =
            typeof msg.windowId === 'number'
              ? msg.windowId
              : typeof sender?.tab?.windowId === 'number'
                ? sender.tab.windowId
                : null;

          if (winId == null) return errRes(M?.ERROR_CODES?.invalidMessage || 'INVALID_MESSAGE', 'Missing window id');

          try {
            await chrome.windows.remove(winId);
            return { ok: true };
          } catch (err) {
            return errRes(classifyErrorCode(err), normalizeErrMsg(err) || 'Failed to close window');
          }
        },

        [M?.TYPES?.translate || 'translate']: async () => {
          const requestId =
            typeof msg.requestId === 'string' && msg.requestId
              ? msg.requestId
              : OM.translationsRepo.createId();

          const sanitizedText = OM.text.sanitize(msg.text);
          await OM.translationsRepo.update(requestId, {
            id: requestId,
            text: sanitizedText,
            translatedText: '',
            source: '',
            target: '',
            status: 'translating',
            at: Date.now(),
          });

          const res = await OM.translatorService.translateFromMessage({
            rawText: msg.text,
            guessedSource: msg.guessedSource,
          });

          if (!res?.ok) {
            await OM.translationsRepo.update(requestId, {
              status: 'error',
              error: res?.error || 'Translate failed',
              errorCode: res?.errorCode || M?.ERROR_CODES?.internal || 'INTERNAL',
              at: Date.now(),
            });
            return { ...res, requestId };
          }

          await OM.translationsRepo.update(requestId, {
            text: res.text,
            translatedText: res.translatedText,
            source: res.source,
            target: res.target,
            status: 'done',
            at: Date.now(),
          });

          return { ok: true, requestId, translatedText: res.translatedText, source: res.source, target: res.target };
        },

        [M?.TYPES?.ttsSpeak || 'ttsSpeak']: async () => {
          return await OM.ttsService.speakFromMessage({ text: msg.text, lang: msg.lang });
        },

        [M?.TYPES?.ttsInfo || 'ttsInfo']: async () => {
          return await OM.ttsService.info();
        },

        [M?.TYPES?.ttsStop || 'ttsStop']: async () => {
          return await OM.ttsService.stop();
        },
      };

      const handler = handlers[type];
      if (!handler) {
        return errRes(M?.ERROR_CODES?.unknownMessageType || 'UNKNOWN_MESSAGE_TYPE', 'Unknown message type');
      }

      try {
        return await handler();
      } catch (err) {
        const errorCode = classifyErrorCode(err);
        return errRes(errorCode, normalizeErrMsg(err) || 'Internal error');
      }
    },
  });
})();

