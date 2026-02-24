(() => {
  const OM = (globalThis.OM = globalThis.OM || {});

  OM.mymemory = Object.freeze({
    async translate({ text, source, target, email }) {
      const de = (email || '').trim();
      const deParam = de ? `&de=${encodeURIComponent(de)}` : '';
      const url =
        `https://api.mymemory.translated.net/get?q=${encodeURIComponent(text)}` +
        `&langpair=${encodeURIComponent(source)}|${encodeURIComponent(target)}` +
        deParam;

      const d = await OM.http.getJson(url);
      const translatedText = d?.responseData?.translatedText;
      if (typeof translatedText !== 'string') throw new Error('Unexpected API response');
      return translatedText;
    },
  });
})();

