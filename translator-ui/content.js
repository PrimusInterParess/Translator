(() => {
    const OM = (globalThis.OM = globalThis.OM || {});
    const C = OM.constants;

    // Default to "off" until we know the real value from storage.
    // This prevents showing any on-page UI while the enabled setting is still loading.
    let enabled = false;
    let enabledLoaded = false;
    let holdToTranslateMs = OM.constants?.DEFAULTS?.holdToTranslateMs ?? 2000;

    let holdTimerId = null;
    let pendingText = '';
    let pendingOpenAt = null;

    const clearHoldTimer = () => {
        if (holdTimerId) clearTimeout(holdTimerId);
        holdTimerId = null;
        pendingText = '';
        pendingOpenAt = null;
    };

    const syncSettingsFromStorage = async () => {
        try {
            const settings = await OM.settings.get();
            enabled = settings.enabled !== false;
            holdToTranslateMs =
                typeof settings.holdToTranslateMs === 'number'
                    ? settings.holdToTranslateMs
                    : OM.constants?.DEFAULTS?.holdToTranslateMs ?? 2000;
        } catch {
            // If we can't read settings for any reason, fail closed: show no on-page UI.
            enabled = false;
        } finally {
            enabledLoaded = true;
        }
    };

    syncSettingsFromStorage().catch(() => { });

    if (globalThis.chrome?.storage?.onChanged?.addListener) {
        chrome.storage.onChanged.addListener((changes, areaName) => {
            if (areaName !== 'local') return;
            const ch = changes?.[C.STORAGE_KEYS.enabled];
            if (!ch) return;
            enabled = ch.newValue !== false;
            enabledLoaded = true;
            if (!enabled) {
                clearHoldTimer();
                OM.contentPopup?.close?.();
            }
        });
    }

    if (globalThis.chrome?.storage?.onChanged?.addListener) {
        chrome.storage.onChanged.addListener((changes, areaName) => {
            if (areaName !== 'local') return;
            const ch = changes?.[C.STORAGE_KEYS.holdToTranslateMs];
            if (!ch) return;
            const ms = typeof ch.newValue === 'number' ? ch.newValue : Number(ch.newValue);
            holdToTranslateMs = Number.isFinite(ms) ? ms : (OM.constants?.DEFAULTS?.holdToTranslateMs ?? 2000);
        });
    }

    document.addEventListener('mouseup', async (e) => {
        if (!enabledLoaded) {
            await syncSettingsFromStorage();
        }
        if (!enabled) return;
        if (OM.contentPopup.isInsideEventTarget(e.target)) return;

        const selected = window.getSelection ? window.getSelection().toString() : '';
        const text = OM.text.sanitize(selected);
        if (!text) {
            clearHoldTimer();
            return;
        }
        if (OM.text.isTooLong(selected)) {
            clearHoldTimer();
            return;
        }

        // Only translate if the same selection is kept for holdToTranslateMs.
        clearHoldTimer();
        pendingText = text;
        pendingOpenAt = { x: e.pageX, y: e.pageY + 15 };
        holdTimerId = setTimeout(async () => {
            if (!enabledLoaded) {
                await syncSettingsFromStorage();
            }
            if (!enabled) return;
            const currentRaw = window.getSelection ? window.getSelection().toString() : '';
            const currentText = OM.text.sanitize(currentRaw);
            if (!currentText) return;
            if (currentText !== pendingText) return;
            if (OM.text.isTooLong(currentRaw)) return;

            OM.contentPopup.openAt({
                x: pendingOpenAt?.x ?? e.pageX,
                y: pendingOpenAt?.y ?? e.pageY + 15,
                text: currentText,
            });

            clearHoldTimer();
        }, holdToTranslateMs);
    });

    document.addEventListener('mousedown', (e) => {
        clearHoldTimer();
        OM.contentPopup.closeIfClickOutside(e.target);
    });

    document.addEventListener('selectionchange', () => {
        if (!holdTimerId) return;
        const current = OM.text.sanitize(window.getSelection ? window.getSelection().toString() : '');
        if (!current || current !== pendingText) clearHoldTimer();
    });
})();
