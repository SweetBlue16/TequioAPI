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
  const suite = collection.item.find((group) => group.name.startsWith('05 -'));
  environment = updateEnvironment(environment, {
    authSelectedCases: 'CP-AUT-01,CP-AUT-04',
    authStage: 'prepare',
    buyerRoleId: 1,
    producerRoleId: 2,
  });
  for (const id of ['CP-AUT-04', 'CP-AUT-01']) {
    const scenario = suite.item.find((group) => group.name.startsWith(id));
    for (const item of scenario.item.slice(0, 2)) {
      try {
        environment = await runItem(collection, environment, item.name, [
          { ...suite, item: [{ ...scenario, item: [item] }] },
        ]);
      } catch (error) {
        return { results, blocked: item.name, reason: String(error.message) };
      }
    }
  }
  return { results };

}

execute()
  .then(async (summary) => {
    await fs.writeFile(path.join(evidence, 'newman-auth-ampliado.json'), JSON.stringify(summary, null, 2));
  })
  .catch(async (error) => {
    await fs.writeFile(path.join(evidence, 'newman-auth-ampliado.json'), JSON.stringify({
      results,
      error: String(error.message),
    }, null, 2));
    process.exitCode = 1;
  });

