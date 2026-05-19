(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  const trim = (x) => String(x ?? '').trim();

  const isExplainResponse = (v) => {
    if (!v || typeof v !== 'object' || v.ok !== true) return false;
    return (
      typeof v.translation === 'string' &&
      typeof v.inYourSentence === 'string' &&
      typeof v.whenUsed === 'string' &&
      typeof v.whyDifferent === 'string' &&
      Array.isArray(v.examples)
    );
  };

  const addCard = (title, text) => {
    const card = document.createElement('div');
    card.className = 'explainCard';
    const t = document.createElement('div');
    t.className = 'explainCardTitle';
    t.textContent = title;
    const p = document.createElement('div');
    p.className = 'explainText';
    p.textContent = trim(text);
    card.append(t, p);
    return card;
  };

  const renderExplain = (container, v) => {
    const sentence = trim(v.meta?.sentence);
    const fragment = trim(v.meta?.fragment);
    const hasFragment = fragment && fragment !== '(none)';
    const sentenceTr = trim(v.sentenceTranslation) || trim(v.translation);
    const partTr = trim(v.translation);

    const hero = document.createElement('div');
    hero.className = 'explainHero';

    const sentenceLabel = document.createElement('div');
    sentenceLabel.className = 'explainHeroLabel';
    sentenceLabel.textContent = 'Sentence';
    hero.appendChild(sentenceLabel);

    const sentenceTranslation = document.createElement('div');
    sentenceTranslation.className = 'explainTranslation';
    sentenceTranslation.textContent = sentenceTr;
    hero.appendChild(sentenceTranslation);

    if (sentence) {
      const sentenceSource = document.createElement('div');
      sentenceSource.className = 'explainSentenceSource';
      sentenceSource.textContent = sentence;
      hero.appendChild(sentenceSource);
    }

    if (hasFragment && partTr && partTr !== sentenceTr) {
      const partLabel = document.createElement('div');
      partLabel.className = 'explainHeroLabel explainHeroLabelPart';
      partLabel.textContent = 'Part';
      hero.appendChild(partLabel);

      const partTranslation = document.createElement('div');
      partTranslation.className = 'explainPartTranslation';
      partTranslation.textContent = partTr;
      hero.appendChild(partTranslation);

      const frag = document.createElement('div');
      frag.className = 'explainFragment';
      frag.textContent = fragment;
      hero.appendChild(frag);
    }

    container.appendChild(hero);
    container.appendChild(addCard('In your sentence', v.inYourSentence));
    container.appendChild(addCard('When it is used', v.whenUsed));
    container.appendChild(addCard('Why it differs elsewhere', v.whyDifferent));

    const examples = Array.isArray(v.examples) ? v.examples : [];
    if (examples.some((ex) => trim(ex?.source))) {
      const wrap = document.createElement('div');
      wrap.className = 'explainExamples';
      for (const ex of examples) {
        const src = trim(ex?.source);
        const meaning = trim(ex?.meaning);
        if (!src || !meaning) continue;
        const card = document.createElement('div');
        card.className = 'explainExample';
        const ctx = trim(ex?.context);
        if (ctx) {
          const c = document.createElement('div');
          c.className = 'explainExampleContext';
          c.textContent = ctx;
          card.appendChild(c);
        }
        const s = document.createElement('div');
        s.className = 'explainExampleSource';
        s.textContent = src;
        const m = document.createElement('div');
        m.className = 'explainExampleMeaning';
        m.textContent = meaning;
        card.append(s, m);
        wrap.appendChild(card);
      }
      const section = document.createElement('div');
      section.className = 'explainCard';
      const t = document.createElement('div');
      t.className = 'explainCardTitle';
      t.textContent = 'Different use cases';
      section.append(t, wrap);
      container.appendChild(section);
    }
  };

  OM.explainRender = Object.freeze({
    isExplainResponse,
    renderExplain,
  });
})();
