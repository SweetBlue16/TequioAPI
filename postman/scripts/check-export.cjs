const assert = require('node:assert/strict');

/** Verifies that an imported event and the tracked JavaScript function have identical bodies. */
const assertExportedScript = (item, eventName, handler) => {
  const event = item.event.find((entry) => entry.listen === eventName);
  const functionSource = Function.prototype.toString.call(handler).replaceAll('\r\n', '\n');
  const body = functionSource.slice(functionSource.indexOf('\n') + 1, functionSource.lastIndexOf('\n'));
  assert.equal(event.script.exec.join('\n'), body, `Postman event differs from checked source: ${eventName}`);
};

module.exports = { assertExportedScript };
