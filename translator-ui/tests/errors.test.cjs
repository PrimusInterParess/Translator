const test = require('node:test');
const assert = require('node:assert/strict');

const { loadScripts } = require('./loadScripts.cjs');

test('OM.errors.friendlyTranslateError maps errorCode when present', () => {
  globalThis.OM = {};
  loadScripts(['shared/om.js', 'shared/errors.js']);

  const msg = globalThis.OM.errors.friendlyTranslateError({ ok: false, errorCode: 'NETWORK', error: 'Failed to fetch' });
  assert.ok(msg.toLowerCase().includes('internet') || msg.toLowerCase().includes('reach'));
});

test('OM.errors.friendlyTranslateError falls back to message classification', () => {
  globalThis.OM = {};
  loadScripts(['shared/om.js', 'shared/errors.js']);

  const msg = globalThis.OM.errors.friendlyTranslateError('Text is too long (max 500 chars).');
  assert.ok(msg.toLowerCase().includes('too long'));
});

