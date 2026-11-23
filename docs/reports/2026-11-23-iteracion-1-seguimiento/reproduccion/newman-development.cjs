const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const { randomUUID } = require('node:crypto');
const path = require('node:path');
const newman = require(path.join(process.env.APPDATA, 'npm/node_modules/newman'));

const [root, evidence, stage] = process.argv.slice(2);
const baseUrl = 'http://localhost:5168';
const runId = randomUUID().replaceAll('-', '');
const results = [];
const sessionKeys = [
  'activeAccount', 'jwt', '_jwtRunId', '_jwtAccount', '_jwtUserId', '_jwtRoleId',
  '_profileRunId', '_profileAccount', '_profileUserId',
];

async function runItem(collection, environment, name, branch = []) {
  const run = await new Promise((resolve, reject) => {
    newman.run({ collection: { ...collection, item: branch }, environment, reporters: [] },
      (error, summary) => {
        if (error) {
          reject(error);
        } else {
          resolve(summary);
        }
      });
  });
  for (const row of run.run.executions) {
    results.push({
      name: row.item.name,
      method: row.request?.method,
      status: row.response?.code ?? null,
      assertions: (row.assertions ?? []).map((result) => ({
        name: result.assertion,
        passed: !result.error && !result.skipped,
      })),
    });
  }
  assert.equal(run.run.stats.assertions.failed, 0, name);
  for (const failure of run.run.failures) {
    const message = String(failure.error?.message ?? '')
      .replace(/eyJ[A-Za-z0-9_.-]+/g, '[redacted]')
      .replace(/\b\d{6}\b/g, '[redacted]');
    results.push({ name, failure: failure.error?.name, message, at: failure.at });
  }
  assert.equal(run.run.failures.length, 0, name);
  assert.ok(run.run.executions.some((row) => row.response), `No HTTP response for ${name}`);
  return run.environment.toJSON();
}

function updateEnvironment(environment, updates) {
  const values = new Map(environment.values.map((entry) => [entry.key, { ...entry }]));
  for (const [key, value] of Object.entries(updates)) {
    values.set(key, { ...values.get(key), key, value: String(value), enabled: true });
  }
  return { ...environment, values: [...values.values()] };
}

async function getCode(mailbox) {
  const file = path.join(stage, mailbox + '.otp');
  for (let index = 0; index < 40; index += 1) {
    const result = await fs.readFile(file, 'utf8').catch(() => '');
    if (/^\d{6}$/.test(result)) {
      await fs.unlink(file);
      return result;
    }
    await new Promise((resolve) => setTimeout(resolve, 150));
  }
  throw new Error('The local inbox did not receive the expected test message.');
}

async function execute() {
  const readJson = async (name) => JSON.parse(await fs.readFile(path.join(root, 'postman', name), 'utf8'));
  const collection = await readJson('Tequio.postman_collection.json');
  const sourceEnvironment = await readJson('Tequio.Development.postman_environment.json');
  let environment = updateEnvironment(sourceEnvironment, {
    baseUrl,
    runId,
    authorizedDevelopmentUrl: baseUrl,
    developmentEnvironmentConfirmed: 'true',
    allowMutatingTests: 'true',
    testInboxAddress: 'test-mailbox@tequio.invalid',
    testInboxConfirmed: 'true',
    emailRoutingConfirmed: 'true',
    buyerEmailTemplate: 'test-buyer-{{runId}}@tequio.invalid',
    producerEmailTemplate: 'test-producer-{{runId}}@tequio.invalid',
    buyerPassword: process.env.TEQUIO_TEST_PASSWORD,
    producerPassword: process.env.TEQUIO_TEST_PASSWORD,
    pageIndex: 1,
    pageSize: 10,
    categoryId: 1,
  });
  const legacyItems = collection.item.slice(0, 5).flatMap((group) => group.item);
  const sessions = new Map();
  const legacy = async (name) => {
    const item = legacyItems.find((candidate) => candidate.name === name);
    assert.ok(item, name);
    environment = await runItem(collection, environment, name, [item]);
  };

  await legacy('Read development OpenAPI document');
  for (const role of ['buyer', 'producer']) {
    environment = updateEnvironment(environment, { activeAccount: role });
    await legacy('Register active test account');
    environment = updateEnvironment(environment, {
      verificationOtp: await getCode(`test-${role}-${runId}`),
    });
    await legacy('Verify account with mailbox OTP');
    await legacy('Log in active test account');
    await legacy('Read my test profile and capture real user ID');
    sessions.set(role, Object.fromEntries(
      environment.values.filter((entry) => sessionKeys.includes(entry.key)).map(({ key, value }) => [key, value])
    ));
  }

  const profiles = collection.item.find((group) => group.name.startsWith('06 -'));
  const source = profiles.event.find((event) => event.listen === 'prerequest').script.exec.join('\n');
  const manifest = JSON.parse(source.match(/^const pcManifest = ([\s\S]*?);\n/)[1]);
  for (const id of ['CP-PER-01', 'CP-PER-02', 'CP-CAT-01', 'CP-CAT-03']) {
    const group = profiles.item.find((entry) => entry.name.startsWith(id));
    environment = updateEnvironment(environment, { pcSelectedCase: id });
    for (const item of group.item) {
      const details = manifest[item.name];
      if (details.action === 'deactivate') {
        environment = updateEnvironment(environment, { confirmDeactivationRunId: runId });
      }
      environment = updateEnvironment(environment, sessions.get(details.role));
      environment = await runItem(collection, environment, item.name, [
        { ...profiles, item: [{ ...group, item: [item] }] },
      ]);
    }
  }

  const evidenceEntry = environment.values.find((entry) => entry.key === 'pcEvidence');
  const caseEntry = environment.values.find((entry) => entry.key === 'pcResults');
  return { results, assertions: JSON.parse(evidenceEntry.value), cases: JSON.parse(caseEntry.value).cases };
}

execute()
  .then(async (summary) => {
    await fs.writeFile(path.join(evidence, 'newman-development.json'), JSON.stringify(summary, null, 2));
  })
  .catch(async (error) => {
    await fs.writeFile(path.join(evidence, 'newman-development.json'), JSON.stringify({
      results,
      error: String(error.message),
    }, null, 2));
    process.exitCode = 1;
  });

