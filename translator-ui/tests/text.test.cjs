const test = require('node:test');
const assert = require('node:assert/strict');

const { loadScripts } = require('./loadScripts.cjs');

test('OM.text.sanitize clamps to MAX_TEXT_LEN', () => {
  globalThis.OM = {};
  loadScripts(['shared/om.js', 'shared/constants.js', 'shared/text.js']);

  const long = 'a'.repeat(globalThis.OM.constants.MAX_TEXT_LEN + 10);
  const out = globalThis.OM.text.sanitize(long);
  assert.equal(out.length, globalThis.OM.constants.MAX_TEXT_LEN);
});

test('OM.text.isTooLong checks trimmed length', () => {
  globalThis.OM = {};
  loadScripts(['shared/om.js', 'shared/constants.js', 'shared/text.js']);

  const ok = 'a'.repeat(globalThis.OM.constants.MAX_TEXT_LEN);
  const tooLong = 'a'.repeat(globalThis.OM.constants.MAX_TEXT_LEN + 1);
  assert.equal(globalThis.OM.text.isTooLong(ok), false);
  assert.equal(globalThis.OM.text.isTooLong(tooLong), true);
});

