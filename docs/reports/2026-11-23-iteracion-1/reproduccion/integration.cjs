const assert = require('node:assert/strict');
const { randomBytes } = require('node:crypto');
const fs = require('node:fs/promises');
const path = require('node:path');
const { execFileSync } = require('node:child_process');
const newman = require(path.join(process.env.APPDATA, 'npm/node_modules/newman'));

const root = process.argv[2];
const out = process.argv[3];
const baseUrl = 'http://localhost:5168/api/v1';
const id = randomBytes(16).toString('hex');
const password = process.env.TEQUIO_TEST_PASSWORD;
const steps = [];

async function request(label, route, method, status, data, token) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  const response = await fetch(baseUrl + route, {
    method, headers, body: data ? JSON.stringify(data) : undefined,
  });
  steps.push({ label, method, route, expected: status, actual: response.status });
  assert.equal(response.status, status, label);
  return await response.json().catch(() => null);
}

async function otpFor(email) {
  const file = path.join(__dirname, email.split('@')[0] + '.otp');
  for (let attempt = 0; attempt < 50; attempt += 1) {
    const code = await fs.readFile(file, 'utf8').catch(() => '');
    if (/^\d{6}$/.test(code)) {
      await fs.unlink(file);
      return code;
    }
    await new Promise((resolve) => setTimeout(resolve, 200));
  }
  throw new Error('No OTP was delivered to the isolated mailbox.');
}

function account(email, roleId) {
  return {
    email, password, firstName: 'Ana', paternalLastName: 'Pruebas',
    birthDate: '1990-02-10', phoneNumber: '2281234567', roleId,
  };
}

async function login(email, secret = password) {
  const result = await request('Sign in with verified test account', '/auth/login',
    'POST', 200, { email, password: secret });
  assert.equal(typeof result.token, 'string');
  return result.token;
}

async function publicAccount(name, roleId) {
  const email = `test-${name}-${id}@tequio.invalid`;
  await request('Register public test profile', '/auth/register', 'POST', 201, account(email, roleId));
  const first = await otpFor(email);
  if (roleId === 1) {
    const incorrect = first === '000000' ? '111111' : '000000';
    for (let attempt = 1; attempt <= 5; attempt += 1) {
      await request('Reject incorrect verification code', '/auth/verify', 'POST', 400,
        { email, verificationCode: incorrect });
    }
    await request('Reject old correct code after attempts run out', '/auth/verify', 'POST', 400,
      { email, verificationCode: first });
    await request('Request another verification code', '/auth/resend-verification-code', 'POST', 200, { email });
  }
  const verificationCode = roleId === 1 ? await otpFor(email) : first;
  await request('Verify public test profile', '/auth/verify', 'POST', 200, { email, verificationCode });
  return login(email);
}

function runCollection(environment) {
  return new Promise((resolve, reject) => {
    newman.run({
      collection: path.join(root, 'postman', 'Tequio.CP-PTO.postman_collection.json'),
      environment,
      reporters: [],
    }, (err, summary) => {
      if (err) return reject(err);
      resolve(summary);
    });
  });
}

async function run() {
  const masterEmail = 'test-master@tequio.invalid';
  const master = await login(masterEmail, process.env.TEQUIO_TEST_MASTER_PASSWORD);
  const adminEmail = `test-admin-${id}@tequio.invalid`;
  await request('Master Admin creates a pending Admin account', '/auth/register-admin',
    'POST', 201, account(adminEmail, 1), master);
  const verificationCode = await otpFor(masterEmail);
  await request('Master Admin authorizes the pending Admin', '/auth/authorize-admin',
    'POST', 200, { newAdminEmail: adminEmail, verificationCode }, master);
  const adminToken = await login(adminEmail);
  await request('Admin role rejected in public registration', '/auth/register',
    'POST', 400, account(`test-forbidden-${id}@tequio.invalid`, 3));
  const nonAdminToken = await publicAccount('buyer', 1);
  const producer = await publicAccount('producer', 2);
  const profile = await request('Read producer profile', '/users/me', 'GET', 200, undefined, producer);
  const { id: producerId } = profile;
  await request('Buyer cannot create catalog products', '/products', 'POST', 403,
    { categoryId: 1, name: 'Denied', shortDescription: 'Denied fixture', measurementUnit: 'kg' }, nonAdminToken);
  const product = await request('Create producer test catalog product', '/products', 'POST', 201,
    { categoryId: 1, name: `TEST_${id}`, shortDescription: 'Producto de prueba', measurementUnit: 'kg' }, producer);
  const { productId } = product;
  const page = await request('Read category and confirm the product persists', '/products/category/1?pageIndex=1&pageSize=100', 'GET', 200);
  assert.ok(page.products.some((item) => item.id === productId));
  assert.ok(Number.isInteger(producerId) && producerId > 0);
  assert.ok(Number.isInteger(productId) && productId > 0);
  await request('Update authenticated producer profile', '/users/me', 'PUT', 200, {
    firstName: 'Ana', paternalLastName: 'Pruebas', birthDate: '1990-02-10',
    phoneNumber: '2281234567', locality: 'Xalapa', biography: 'Perfil ficticio de integración',
  }, producer);
  const updated = await request('Read updated producer profile', '/users/me', 'GET', 200, undefined, producer);
  assert.equal(updated.biography, 'Perfil ficticio de integración');
  assert.equal(updated.id, producerId);
  const buyerEmail = `test-buyer-${id}@tequio.invalid`;
  await request('Request password recovery through the isolated inbox', '/auth/recover', 'POST', 200, { email: buyerEmail });
  const recoveryCode = await otpFor(buyerEmail);
  const newPassword = process.env.TEQUIO_TEST_NEW_PASSWORD;
  await request('Reset password with received OTP', '/auth/reset-password', 'POST', 200, {
    email: buyerEmail, code: recoveryCode, newPassword,
  });
  await request('Reject old password after reset', '/auth/login', 'POST', 401, { email: buyerEmail, password });
  await login(buyerEmail, newPassword);
  await request('Reject reused recovery OTP', '/auth/reset-password', 'POST', 400, {
    email: buyerEmail, code: recoveryCode, newPassword,
  });
  const fixture = (await fs.readFile(path.join(root, 'postman', 'delivery-points-fixture.sql'), 'utf8'))
    .replace('REPLACE_WITH_32_HEX_CHARACTERS', id)
    .replace('DECLARE @producerId INT = 0', `DECLARE @producerId INT = ${producerId}`)
    .replace('DECLARE @baseProductId INT = 0', `DECLARE @baseProductId INT = ${productId}`)
    .replace("FirstName LIKE N'TEST[_]%'", `Email = N'test-producer-${id}@tequio.invalid'`);
  const fixtureFile = path.join(__dirname, 'rn07-fixture.sql');
  await fs.writeFile(fixtureFile, 'SET NOCOUNT ON;\n' + fixture);
  const rows = execFileSync('sqlcmd', ['-S', '.\\SQLEXPRESS', '-E', '-C', '-b', '-f', '65001',
    '-d', process.env.TEQUIO_TEST_DATABASE, '-h', '-1', '-W', '-s', ',', '-i', fixtureFile], { encoding: 'utf8' });
  const [linkedDeliveryPointId, linkedBatchId, municipalityId] = rows.trim().split(',').map(Number);
  assert.ok(linkedBatchId > 0 && linkedDeliveryPointId > 0 && municipalityId > 0);
  await request('Existing active batch prevents product deactivation', '/products/' + productId + '/deactive',
    'PATCH', 409, undefined, producer);
  const unlinked = await request('Create a separate product with no batches', '/products', 'POST', 201,
    { categoryId: 1, name: `TEST_${id}_unlinked`, shortDescription: 'Producto de prueba', measurementUnit: 'kg' }, producer);
  await request('Deactivate only the unlinked product', '/products/' + unlinked.productId + '/deactive',
    'PATCH', 200, undefined, producer);
  const remaining = await request('Check both product states through the catalog', '/products/category/1?pageIndex=1&pageSize=100', 'GET', 200);
  assert.ok(remaining.products.some((item) => item.id === productId));
  assert.ok(!remaining.products.some((item) => item.id === unlinked.productId));
  const producerDenied = await fetch(baseUrl + '/delivery-points/' + linkedDeliveryPointId + '/deactive', {
    method: 'PATCH', headers: { Authorization: `Bearer ${producer}` },
  });
  steps.push({ label: 'Producer denied administrative action', expected: 403, actual: producerDenied.status });
  assert.equal(producerDenied.status, 403);
  const inputs = {
    baseUrl, adminToken, nonAdminToken, municipalityId, municipalityName: 'Xalapa',
    linkedDeliveryPointId, linkedBatchId, missingDeliveryPointId: 2147483647,
  };
  const environment = { values: Object.entries(inputs).map(([key, value]) => ({ key, value: String(value) })) };
  const summary = await runCollection(environment);
  const executions = summary.run.executions.map((item) => ({
    id: item.item.id,
    name: item.item.name,
    method: item.request.method,
    status: item.response?.code,
    assertions: item.assertions?.map((assertion) => ({
      name: assertion.assertion, passed: !assertion.error && !assertion.skipped,
    })),
  }));
  await fs.writeFile(path.join(out, 'newman-cp-pto.json'), JSON.stringify({
    stats: summary.run.stats, failures: summary.run.failures.map((entry) => ({
      source: entry.source?.name, assertion: entry.error?.test, type: entry.error?.name,
    })), executions,
  }, null, 2));
  if (summary.run.failures.length) process.exitCode = 1;
}

run().catch((error) => {
  steps.push({ failure: error.message });
  process.exitCode = 1;
}).finally(() => fs.writeFile(path.join(out, 'integration-http.json'), JSON.stringify(steps, null, 2)));

