const test = require('node:test');
const assert = require('node:assert/strict');

const { loadScripts } = require('./loadScripts.cjs');

test('OM.lang.guessSourceLang falls back to unicode heuristics without document', () => {
  globalThis.OM = {};
  loadScripts(['shared/om.js', 'shared/lang.js']);

  assert.equal(globalThis.OM.lang.guessSourceLang('hello'), 'en');
  assert.equal(globalThis.OM.lang.guessSourceLang('привет'), 'bg'); // Cyrillic default
  assert.equal(globalThis.OM.lang.guessSourceLang('مرحبا'), 'ar');
});

