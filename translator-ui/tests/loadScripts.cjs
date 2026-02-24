const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function loadScripts(relPaths) {
  for (const rel of relPaths) {
    const abs = path.join(__dirname, '..', rel);
    const code = fs.readFileSync(abs, 'utf8');
    vm.runInThisContext(code, { filename: abs });
  }
}

module.exports = { loadScripts };

