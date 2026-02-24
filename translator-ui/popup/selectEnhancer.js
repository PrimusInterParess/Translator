(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  function enhanceSelect(selectEl, { onChange }) {
    const wrap = selectEl.closest('.selectWrap');
    const btn = wrap.querySelector('.selectBtn');
    const list = wrap.querySelector('.selectList');

    // Portal the dropdown to <body> so it doesn't "grow" the card.
    if (list.parentElement !== document.body) {
      document.body.appendChild(list);
    }

    const positionList = () => {
      const rect = btn.getBoundingClientRect();
      const margin = 8;
      const extraWidth = 6; // slightly outside the button
      const gap = 6;

      const width = Math.min(rect.width + extraWidth * 2, window.innerWidth - margin * 2);
      const idealLeft = rect.left + rect.width / 2 - width / 2;
      const left = Math.max(margin, Math.min(idealLeft, window.innerWidth - margin - width));

      list.style.left = `${left}px`;
      list.style.width = `${width}px`;

      const downTop = rect.bottom + gap;
      const availableDown = window.innerHeight - downTop - margin;
      const availableUp = rect.top - gap - margin;

      // Prefer the direction with more space. If there isn't enough space below, flip upward.
      const openUp = availableDown < 120 && availableUp > availableDown;

      if (openUp) {
        // Anchor above the button.
        const bottom = window.innerHeight - rect.top + gap;
        list.style.top = 'auto';
        list.style.bottom = `${Math.max(margin, bottom)}px`;
        list.style.maxHeight = `${Math.max(40, Math.max(0, availableUp))}px`;
      } else {
        // Anchor under the button.
        list.style.bottom = 'auto';
        list.style.top = `${downTop}px`;
        list.style.maxHeight = `${Math.max(40, Math.max(0, availableDown))}px`;
      }
    };

    const setOpen = (open) => {
      // close other open dropdowns
      if (open) {
        document.querySelectorAll('.selectWrap[data-open="true"]').forEach((w) => {
          if (w !== wrap) w.dataset.open = 'false';
        });
        document.querySelectorAll('.selectList').forEach((l) => {
          if (l !== list) l.style.display = 'none';
        });
      }
      wrap.dataset.open = open ? 'true' : 'false';
      btn.setAttribute('aria-expanded', open ? 'true' : 'false');
      list.style.display = open ? 'block' : 'none';
      if (open) positionList();
    };

    const render = () => {
      list.innerHTML = '';
      const selectedValue = selectEl.value;
      const selectedOption =
        Array.from(selectEl.options).find((o) => o.value === selectedValue) || selectEl.options[0];
      btn.textContent = selectedOption ? selectedOption.textContent : '';

      for (const opt of Array.from(selectEl.options)) {
        const item = document.createElement('div');
        item.className = 'selectOption';
        item.setAttribute('role', 'option');
        item.dataset.value = opt.value;
        item.dataset.selected = opt.value === selectedValue ? 'true' : 'false';
        item.textContent = opt.textContent;
        item.addEventListener('click', async () => {
          selectEl.value = opt.value;
          await onChange(opt.value);
          render();
          setOpen(false);
        });
        list.appendChild(item);
      }
    };

    btn.addEventListener('click', () => {
      const isOpen = wrap.dataset.open === 'true';
      setOpen(!isOpen);
    });

    document.addEventListener('mousedown', (e) => {
      if (!wrap.contains(e.target) && !list.contains(e.target)) setOpen(false);
    });

    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') setOpen(false);
    });

    window.addEventListener('resize', () => {
      if (wrap.dataset.open === 'true') positionList();
    });

    render();

    return { render, setOpen };
  }

  OM.popupSelectEnhancer = Object.freeze({ enhanceSelect });
})();

