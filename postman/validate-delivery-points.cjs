// Structural and JavaScript syntax validation only; this is not evidence of HTTP/SQL execution.
const fs = require('node:fs');
const path = require('node:path');
const { execFileSync } = require('node:child_process');
const assert = require('node:assert/strict');
const collection = JSON.parse(
  fs.readFileSync(path.join(__dirname, 'Tequio.CP-PTO.postman_collection.json'), 'utf8')
);
const environment = JSON.parse(
  fs.readFileSync(path.join(__dirname, 'Tequio.CP-PTO.example.postman_environment.json'), 'utf8')
);
assert.equal(
  collection.info.schema,
  'https://schema.getpostman.com/json/collection/v2.1.0/collection.json'
);
const names = new Set();
for (const item of collection.item) {
  assert.ok(!names.has(item.name), item.name);
  names.add(item.name);
  assert.ok(item.request.url.startsWith('{{baseUrl}}/delivery-points'));
  assert.ok(
    item.event.some(
      (event) =>
        event.listen === 'test' &&
        event.script.exec.join('\n').includes('pm.response.to.have.status')
    )
  );
  for (const event of item.event) {
    execFileSync(process.execPath, ['--check', '-'], {
      input: event.script.exec.join('\n'),
    });
  }
}
for (const key of ['adminToken', 'nonAdminToken']) {
  assert.equal(environment.values.find((value) => value.key === key).value, '');
}
for (let index = 1; index <= 13; index++) {
  assert.ok(
    collection.item.some((item) =>
      item.name.startsWith('CP-PTO-' + String(index).padStart(2, '0') + ' ')
    )
  );
}
assert.ok(
  collection.item
    .find((item) => item.name.startsWith('CP-PTO-05 |'))
    .event.some(
      (event) =>
        event.listen === 'prerequest' && event.script.exec.join('\n').includes('throw new Error')
    )
);
process.stdout.write(
  `Colección y entorno válidos: ${collection.item.length} solicitudes; scripts compilados; sin ejecutar HTTP/SQL.\n`
);
